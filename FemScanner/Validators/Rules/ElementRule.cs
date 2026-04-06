using FemScanner.Models;

namespace FemScanner.Validators.Rules;

/// <summary>Element 카드 검증 규칙 (노드 참조 무결성, Property 참조 무결성)</summary>
public class ElementRule : IValidationRule
{
    public IEnumerable<ValidationResult> Validate(BdfModel model)
    {
        var gridIds = model.Grids.Select(g => g.Id).ToHashSet();
        var propertyIds = model.Properties.Select(p => p.Id).ToHashSet();

        foreach (var element in model.Elements)
        {
            // 노드 ID → Grid 참조 무결성
            for (int i = 0; i < element.NodeIds.Length; i++)
            {
                int nodeId = element.NodeIds[i];
                if (nodeId != 0 && !gridIds.Contains(nodeId))
                {
                    yield return new ValidationResult
                    {
                        Severity = ValidationSeverity.Error,
                        CardType = element.CardType,
                        CardId = element.Id,
                        FieldName = $"G{i + 1}",
                        Message = $"{element.CardType} {element.Id}: 노드 ID {nodeId}에 해당하는 GRID가 존재하지 않습니다.",
                    };
                }
            }

            // PropertyId → Property 참조 무결성
            if (element.PropertyId != 0 && !propertyIds.Contains(element.PropertyId))
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    CardType = element.CardType,
                    CardId = element.Id,
                    FieldName = "PID",
                    Message = $"{element.CardType} {element.Id}: Property ID {element.PropertyId}가 존재하지 않습니다.",
                };
            }
        }
    }
}
