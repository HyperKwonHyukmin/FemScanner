using System.Text.Json.Serialization;

namespace FemScanner.Models;

/// <summary>단계별 검증 보고서 루트</summary>
public class ValidationReport
{
    public string Version { get; set; } = "1.0";

    /// <summary>1 = BDF 기본 검토, 2 = Nastran 해석 검토</summary>
    public int Step { get; set; }

    /// <summary>"BDF 기본 검토" | "Nastran 해석 검토"</summary>
    public string StepName { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; set; }
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>"pass" | "warning" | "error"</summary>
    public string Status { get; set; } = "pass";

    public ValidationSummary Summary { get; set; } = new();

    // ── Step 1 전용 필드 (Step 2에서는 null → JSON 생략) ──────────────────────

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParsingSummary? ParsingSummary { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RuleCheckResult>? RulesChecked { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ValidationResult>? ValidationResults { get; set; }

    // ── Step 2 전용 필드 (Step 1에서는 null → JSON 생략) ──────────────────────

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public F06Section? F06Summary { get; set; }
}

/// <summary>오류/경고 카운트 요약</summary>
public class ValidationSummary
{
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public int ParserWarnings { get; set; }
    public int F06Fatals { get; set; }
    public int F06Warnings { get; set; }
}

/// <summary>검증 규칙별 실행 결과</summary>
public class RuleCheckResult
{
    public string Rule { get; set; } = string.Empty;

    /// <summary>"pass" | "warning" | "error"</summary>
    public string Status { get; set; } = "pass";

    /// <summary>검사 대상 카드 수</summary>
    public int CheckedCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}

/// <summary>BDF 파싱 결과 요약 (Step 1)</summary>
public class ParsingSummary
{
    public Dictionary<string, int> CardCounts { get; set; } = new();
    public Dictionary<string, int> ElementBreakdown { get; set; } = new();
    public Dictionary<string, int> PropertyBreakdown { get; set; } = new();
    public Dictionary<string, int> MaterialBreakdown { get; set; } = new();
    public Dictionary<string, int> LoadBreakdown { get; set; } = new();
    public Dictionary<string, int> BcBreakdown { get; set; } = new();
    public List<string> ParserWarnings { get; set; } = [];

    /// <summary>모델 좌표 범위</summary>
    public BoundingBox BoundingBox { get; set; } = new();

    /// <summary>어떤 요소/하중/경계조건에도 참조되지 않는 GRID 수</summary>
    public int OrphanNodes { get; set; }

    /// <summary>어떤 요소에도 참조되지 않는 Property 수</summary>
    public int OrphanProperties { get; set; }

    /// <summary>어떤 Property에도 참조되지 않는 Material 수</summary>
    public int OrphanMaterials { get; set; }
}

/// <summary>모델 좌표 범위 (GRID 기반)</summary>
public class BoundingBox
{
    public double XMin { get; set; }
    public double XMax { get; set; }
    public double YMin { get; set; }
    public double YMax { get; set; }
    public double ZMin { get; set; }
    public double ZMax { get; set; }
}

/// <summary>F06 파싱 결과 섹션 (Step 2)</summary>
public class F06Section
{
    public int FatalCount { get; set; }
    public int WarningCount { get; set; }
    public List<F06MessageDto> Messages { get; set; } = [];
}

/// <summary>F06 개별 메시지 (직렬화용 DTO)</summary>
public class F06MessageDto
{
    /// <summary>"fatal" | "warning"</summary>
    public string Level { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}
