using System.Text.RegularExpressions;
using FemScanner.Models;

namespace FemScanner.Parsers;

/// <summary>Case Control 섹션 파서 (BEGIN BULK 이전 라인 처리)</summary>
public static class CaseControlParser
{
    // 키워드 = 값 패턴: LOAD = 10, DISP(PRINT) = ALL 등
    private static readonly Regex DirectiveRegex =
        new(@"^\s*(\w+)\s*(?:\([^)]*\))?\s*=\s*(.+?)\s*$", RegexOptions.Compiled);

    // SUBCASE n 패턴
    private static readonly Regex SubcaseRegex =
        new(@"^\s*SUBCASE\s+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Case Control 섹션 라인들을 파싱하여 CaseControl 모델을 반환합니다.</summary>
    public static CaseControl Parse(IEnumerable<string> lines)
    {
        var caseControl = new CaseControl();
        Subcase? currentSubcase = null;

        foreach (var line in lines)
        {
            // 주석 및 빈 라인 스킵
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('$'))
                continue;

            // SUBCASE 블록 시작
            var subcaseMatch = SubcaseRegex.Match(line);
            if (subcaseMatch.Success)
            {
                currentSubcase = new Subcase { Id = int.Parse(subcaseMatch.Groups[1].Value) };
                caseControl.Subcases.Add(currentSubcase);
                continue;
            }

            // 키워드 = 값 지시문
            var directiveMatch = DirectiveRegex.Match(line);
            if (directiveMatch.Success)
            {
                string key = directiveMatch.Groups[1].Value.Trim().ToUpperInvariant();
                string value = directiveMatch.Groups[2].Value.Trim();

                if (currentSubcase != null)
                    currentSubcase.Directives[key] = value;
                else
                    caseControl.GlobalDirectives[key] = value;
            }
        }

        return caseControl;
    }
}
