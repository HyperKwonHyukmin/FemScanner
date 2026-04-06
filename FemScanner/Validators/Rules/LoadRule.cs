using FemScanner.Models;
using FemScanner.Models.Loads;

namespace FemScanner.Validators.Rules;

/// <summary>Load 카드 검증 규칙 (노드 참조 무결성)</summary>
public class LoadRule : IValidationRule
{
    public IEnumerable<ValidationResult> Validate(BdfModel model)
    {
        var gridIds = model.Grids.Select(g => g.Id).ToHashSet();

        foreach (var load in model.Loads)
        {
            int? nodeId = GetNodeId(load);
            if (nodeId.HasValue && nodeId.Value != 0 && !gridIds.Contains(nodeId.Value))
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    CardType = load.CardType,
                    CardId = load.Id,
                    FieldName = "G",
                    Message = $"{load.CardType} {load.Id}: 노드 ID {nodeId.Value}에 해당하는 GRID가 존재하지 않습니다.",
                };
            }
        }
    }

    private static int? GetNodeId(ILoad load) => load switch
    {
        Force f => f.NodeId,
        Moment m => m.NodeId,
        _ => null
    };
}
