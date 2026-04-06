using FemScanner.Models;

namespace FemScanner.Validators;

/// <summary>BDF 검증 규칙 인터페이스</summary>
public interface IValidationRule
{
    IEnumerable<ValidationResult> Validate(BdfModel model);
}
