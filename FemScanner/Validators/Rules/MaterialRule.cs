using FemScanner.Models;
using FemScanner.Models.Materials;

namespace FemScanner.Validators.Rules;

/// <summary>Material 카드 검증 규칙 (필수 물성값 확인)</summary>
public class MaterialRule : IValidationRule
{
    public IEnumerable<ValidationResult> Validate(BdfModel model)
    {
        foreach (var mat in model.Materials)
        {
            if (mat is Mat1 mat1)
            {
                // E 또는 G 중 하나는 정의되어야 함
                if (mat1.E == 0 && mat1.G == 0)
                {
                    yield return new ValidationResult
                    {
                        Severity = ValidationSeverity.Warning,
                        CardType = "MAT1",
                        CardId = mat1.Id,
                        FieldName = "E/G",
                        Message = $"MAT1 {mat1.Id}: E와 G 모두 0입니다. 탄성계수를 확인하세요.",
                    };
                }
            }
        }
    }
}
