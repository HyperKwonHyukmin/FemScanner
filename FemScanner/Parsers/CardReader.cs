namespace FemScanner.Parsers;

/// <summary>
/// BDF 파일의 각 라인을 읽어 fixed-field / free-field / large-field 방식으로 토큰 배열로 분리합니다.
/// </summary>
public static class CardReader
{
    /// <summary>
    /// 단일 BDF 라인을 토큰 배열로 분리합니다.
    /// </summary>
    /// <param name="line">BDF 라인 문자열</param>
    /// <returns>토큰 배열. 주석 또는 빈 라인이면 빈 배열 반환.</returns>
    public static string[] ReadTokens(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('$'))
            return [];

        // free-field: 콤마 포함 시
        if (line.Contains(','))
        {
            return line.Split(',')
                       .Select(t => t.Trim())
                       .Where(t => t.Length > 0)
                       .ToArray();
        }

        // large-field: 카드명에 * 포함 시 (16자 필드)
        if (line.TrimStart().StartsWith('*') || (line.Length >= 1 && line[0] == '*'))
        {
            return ReadLargeField(line);
        }

        // fixed-field: 8자 슬라이싱
        return ReadFixedField(line);
    }

    private static string[] ReadFixedField(string line)
    {
        var tokens = new List<string>();
        int totalFields = 10; // 카드명(1) + 데이터(8) + continuation(1)

        for (int i = 0; i < totalFields; i++)
        {
            int start = i * 8;
            if (start >= line.Length)
                break;

            int length = Math.Min(8, line.Length - start);
            string token = line.Substring(start, length).Trim();
            tokens.Add(token);
        }

        // 끝에서부터 빈 토큰 제거
        while (tokens.Count > 0 && tokens[^1].Length == 0)
            tokens.RemoveAt(tokens.Count - 1);

        return tokens.ToArray();
    }

    private static string[] ReadLargeField(string line)
    {
        var tokens = new List<string>();

        // 첫 번째 필드: 카드명 8자
        string cardName = line.Length >= 8 ? line.Substring(0, 8).Trim() : line.Trim();
        if (cardName.Length > 0)
            tokens.Add(cardName);

        // 이후 필드: 16자씩
        int pos = 8;
        while (pos < line.Length)
        {
            int length = Math.Min(16, line.Length - pos);
            string token = line.Substring(pos, length).Trim();
            if (token.Length > 0)
                tokens.Add(token);
            pos += 16;
        }

        return tokens.ToArray();
    }

    /// <summary>
    /// 여러 BDF 라인을 읽어 continuation 카드를 병합한 뒤 단일 토큰 배열로 반환합니다.
    /// continuation 판별: 두 번째 필드가 '+' 또는 공백이면 이전 카드에 병합합니다.
    /// </summary>
    /// <param name="lines">BDF 라인 목록</param>
    /// <returns>병합된 카드별 토큰 배열 목록</returns>
    public static List<string[]> ReadCards(IEnumerable<string> lines)
    {
        var result = new List<string[]>();
        List<string>? current = null;

        foreach (var line in lines)
        {
            var tokens = ReadTokens(line);
            if (tokens.Length == 0)
                continue;

            // continuation 판별: 두 번째 필드(index 1)가 '+' 이거나 비어있고 첫 필드도 비어있으면 continuation
            bool isContinuation = current != null &&
                (tokens[0].StartsWith('+') || tokens[0].Length == 0 ||
                 (tokens.Length > 1 && (tokens[1].StartsWith('+') || tokens[1] == string.Empty)));

            if (isContinuation && current != null)
            {
                // continuation 마커(tokens[0] 또는 tokens[1])를 제외하고 데이터 필드만 병합
                // fixed-field: tokens[0]=카드명/마커, tokens[1]=continuation마커, tokens[2..]= 데이터
                // continuation 라인의 첫 필드(마커)와 두 번째 필드(마커) 제외
                int skipCount = tokens[0].StartsWith('+') || tokens[0].Length == 0 ? 1 : 2;
                for (int i = skipCount; i < tokens.Length; i++)
                {
                    if (tokens[i].Length > 0)
                        current.Add(tokens[i]);
                }
            }
            else
            {
                if (current != null)
                    result.Add(current.ToArray());
                current = new List<string>(tokens);
            }
        }

        if (current != null)
            result.Add(current.ToArray());

        return result;
    }
}
