using FemScanner.Models;

namespace FemScanner.Helpers;

/// <summary>
/// Nastran 모델 구조 검증용 임시 BDF 라인 배열을 생성합니다.
/// 원본 BDF에서 하중 카드(FORCE, MOMENT, PLOAD, PLOAD4)를 제거하고
/// GRAV만 남기거나 없으면 기본 GRAV를 추가합니다.
/// 이를 통해 하중 오류 없이 모델 자체의 구조적 유효성만 검증합니다.
/// </summary>
public static class BdfCheckFileBuilder
{
    // GRAV가 없을 때 삽입할 기본 카드: SID=99999, CID=0, G=9.81, Z축 하향(-1.0)
    private const string DefaultGravCard = "GRAV,99999,0,9.81,0.0,0.0,-1.0";
    private const int DefaultGravSid = 99999;

    // 제거 대상 하중 카드명 (대소문자 무시용 — 이미 대문자로 비교)
    private static readonly HashSet<string> LoadCardsToRemove =
        new(StringComparer.OrdinalIgnoreCase) { "FORCE", "MOMENT", "PLOAD", "PLOAD4" };

    /// <summary>
    /// 원본 BDF 라인 배열을 받아 검증용 임시 BDF 라인 배열을 반환합니다.
    /// </summary>
    /// <param name="originalLines">원본 BDF 파일 라인 배열</param>
    /// <param name="model">파싱된 BdfModel (GRAV 존재 여부 확인용)</param>
    public static string[] BuildCheckLines(string[] originalLines, BdfModel model)
    {
        // 기존 GRAV 확인 — SID는 첫 번째 GRAV 카드의 Id를 사용
        bool hasGrav = model.Loads.Any(l => l.CardType == "GRAV");
        int gravSid = hasGrav
            ? model.Loads.First(l => l.CardType == "GRAV").Id
            : DefaultGravSid;

        // BEGIN BULK / ENDDATA 경계 파악
        int beginBulkIndex = -1;
        int endDataIndex = -1;
        for (int i = 0; i < originalLines.Length; i++)
        {
            string trimmed = originalLines[i].Trim();
            if (beginBulkIndex < 0 &&
                trimmed.StartsWith("BEGIN BULK", StringComparison.OrdinalIgnoreCase))
            {
                beginBulkIndex = i;
            }
            else if (trimmed.Equals("ENDDATA", StringComparison.OrdinalIgnoreCase))
            {
                endDataIndex = i;
                break;
            }
        }

        var result = new List<string>(originalLines.Length);
        bool skipNext = false; // continuation 카드 제거 플래그

        for (int i = 0; i < originalLines.Length; i++)
        {
            string line = originalLines[i];

            // ENDDATA 직전에 기본 GRAV 삽입 (GRAV가 없을 경우)
            if (!hasGrav && i == endDataIndex)
            {
                result.Add(DefaultGravCard);
            }

            bool inBulk = beginBulkIndex >= 0 && i > beginBulkIndex &&
                          (endDataIndex < 0 || i < endDataIndex);

            if (inBulk)
            {
                if (skipNext)
                {
                    // 직전 하중 카드의 continuation 라인 — 제거
                    skipNext = IsContinuationLine(line);
                    continue;
                }

                string cardName = ExtractCardName(line);
                if (LoadCardsToRemove.Contains(cardName))
                {
                    // 다음 줄이 continuation이면 같이 제거
                    skipNext = (i + 1 < originalLines.Length) &&
                               IsContinuationLine(originalLines[i + 1]);
                    continue;
                }
            }
            else if (!inBulk && beginBulkIndex >= 0 && i < beginBulkIndex)
            {
                // Case Control 영역: LOAD = N 디렉티브를 gravSid로 교체
                line = ReplaceLoadDirective(line, gravSid);
            }

            skipNext = false;
            result.Add(line);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Case Control 라인의 LOAD = N 디렉티브를 gravSid로 교체합니다.
    /// </summary>
    private static string ReplaceLoadDirective(string line, int gravSid)
    {
        // "LOAD = 숫자" 또는 "LOAD(...)=숫자" 패턴을 gravSid로 교체
        string trimmed = line.TrimStart();
        if (!trimmed.StartsWith("LOAD", StringComparison.OrdinalIgnoreCase))
            return line;

        int eqIdx = trimmed.IndexOf('=');
        if (eqIdx < 0) return line;

        // 들여쓰기 보존
        int indent = line.Length - trimmed.Length;
        string prefix = line[..indent];
        string keyword = trimmed[..eqIdx].TrimEnd(); // "LOAD" 또는 "LOAD(...)"

        return $"{prefix}{keyword} = {gravSid}";
    }

    /// <summary>
    /// BDF 라인에서 카드명(첫 필드)을 추출합니다. Large-field(*)나 free-field 모두 지원.
    /// </summary>
    private static string ExtractCardName(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('$'))
            return string.Empty;

        // Free-field (콤마 구분)
        if (line.Contains(','))
        {
            string name = line.Split(',')[0].Trim();
            return name.TrimEnd('*').ToUpperInvariant();
        }

        // Fixed-field / Large-field: 첫 8자
        string field = line.Length >= 8 ? line[..8] : line;
        return field.Trim().TrimEnd('*').ToUpperInvariant();
    }

    /// <summary>
    /// 해당 라인이 continuation 카드인지 판별합니다.
    /// '+' 또는 '*'로 시작하거나, 첫 필드가 공백인 경우(fixed-field continuation).
    /// </summary>
    private static bool IsContinuationLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('$'))
            return false;

        char first = line[0];
        if (first == '+' || first == '*')
            return true;

        // Fixed-field: 첫 번째 필드(8자)가 공백이면 continuation
        if (first == ' ')
        {
            string field = line.Length >= 8 ? line[..8] : line;
            return string.IsNullOrWhiteSpace(field);
        }

        // Free-field continuation: 첫 토큰이 비어있거나 '+'로 시작
        if (line.Contains(','))
        {
            string token = line.Split(',')[0].Trim();
            return token.Length == 0 || token.StartsWith('+');
        }

        return false;
    }
}
