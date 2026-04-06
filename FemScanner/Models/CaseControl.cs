namespace FemScanner.Models;

/// <summary>SUBCASE 블록 모델</summary>
public class Subcase
{
    public int Id { get; set; }
    public Dictionary<string, string> Directives { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Case Control 섹션 모델 (BEGIN BULK 이전)</summary>
public class CaseControl
{
    public Dictionary<string, string> GlobalDirectives { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Subcase> Subcases { get; } = [];
}
