namespace FemScanner.Parsers;

/// <summary>F06 메시지 심각도</summary>
public enum F06Level { Fatal, Warning }

/// <summary>F06 개별 메시지 항목</summary>
public class F06Message
{
    public F06Level Level { get; set; }
    public int LineNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    /// <summary>전후 2라인 포함 컨텍스트</summary>
    public string Context { get; set; } = string.Empty;
}

/// <summary>F06 파싱 결과</summary>
public class F06Result
{
    public List<F06Message> Messages { get; } = [];
    public int FatalCount => Messages.Count(m => m.Level == F06Level.Fatal);
    public int WarningCount => Messages.Count(m => m.Level == F06Level.Warning);
}

/// <summary>
/// Nastran F06 결과 파일에서 FATAL / WARNING / USER WARNING 메시지를 추출합니다.
/// </summary>
public class F06Parser
{
    /// <summary>F06 파일을 파싱하여 F06Result를 반환합니다.</summary>
    /// <param name="f06Path">F06 파일 경로</param>
    /// <exception cref="FileNotFoundException">F06 파일이 존재하지 않을 때</exception>
    public F06Result Parse(string f06Path)
    {
        if (!File.Exists(f06Path))
            throw new FileNotFoundException($"F06 파일을 찾을 수 없습니다: {f06Path}", f06Path);

        string[] lines = File.ReadAllLines(f06Path);
        return ParseLines(lines);
    }

    /// <summary>F06 라인 배열을 파싱합니다 (테스트용).</summary>
    public F06Result ParseLines(string[] lines)
    {
        var result = new F06Result();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            F06Level? level = DetectLevel(line);
            if (level is null) continue;

            int contextStart = Math.Max(0, i - 2);
            int contextEnd = Math.Min(lines.Length - 1, i + 2);
            string context = string.Join(Environment.NewLine,
                                           lines[contextStart..(contextEnd + 1)]);

            result.Messages.Add(new F06Message
            {
                Level = level.Value,
                LineNumber = i + 1,
                Message = line.Trim(),
                Context = context,
            });
        }

        return result;
    }

    private static F06Level? DetectLevel(string line)
    {
        // USER WARNING 먼저 확인 (WARNING의 하위 문자열이므로)
        if (line.Contains("USER WARNING", StringComparison.OrdinalIgnoreCase))
            return F06Level.Warning;
        if (line.Contains("FATAL", StringComparison.OrdinalIgnoreCase))
            return F06Level.Fatal;
        if (line.Contains("WARNING", StringComparison.OrdinalIgnoreCase))
            return F06Level.Warning;
        return null;
    }
}
