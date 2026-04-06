namespace FemScanner.Models;

/// <summary>검증 결과 심각도</summary>
public enum ValidationSeverity
{
    Error,
    Warning
}

/// <summary>BDF 검증 결과 항목</summary>
public class ValidationResult
{
    public ValidationSeverity Severity { get; set; }
    public string CardType { get; set; } = string.Empty;
    public int CardId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
