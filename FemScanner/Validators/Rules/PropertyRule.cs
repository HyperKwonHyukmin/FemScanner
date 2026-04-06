using FemScanner.Models;
using FemScanner.Models.Properties;

namespace FemScanner.Validators.Rules;

/// <summary>Property 카드 검증 규칙 (MaterialId 참조 무결성)</summary>
public class PropertyRule : IValidationRule
{
    public IEnumerable<ValidationResult> Validate(BdfModel model)
    {
        var materialIds = model.Materials.Select(m => m.Id).ToHashSet();

        foreach (var prop in model.Properties)
        {
            int matId = GetMaterialId(prop);
            if (matId != 0 && !materialIds.Contains(matId))
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    CardType = prop.CardType,
                    CardId = prop.Id,
                    FieldName = "MID",
                    Message = $"{prop.CardType} {prop.Id}: Material ID {matId}가 존재하지 않습니다.",
                };
            }
        }
    }

    private static int GetMaterialId(IProperty prop) => prop switch
    {
        PShell p => p.MaterialId,
        PSolid p => p.MaterialId,
        PBar p => p.MaterialId,
        PBarL p => p.MaterialId,
        PBeam p => p.MaterialId,
        PBeamL p => p.MaterialId,
        PRod p => p.MaterialId,
        _ => 0
    };
}
