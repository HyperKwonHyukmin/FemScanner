using FemScanner.Models;
using FemScanner.Models.BoundaryConditions;

namespace FemScanner.Validators.Rules;

/// <summary>BoundaryCondition 카드 검증 규칙 (노드 참조 무결성)</summary>
public class BcRule : IValidationRule
{
    public IEnumerable<ValidationResult> Validate(BdfModel model)
    {
        var gridIds = model.Grids.Select(g => g.Id).ToHashSet();

        foreach (var bc in model.BoundaryConditions)
        {
            switch (bc)
            {
                case Spc spc:
                    if (spc.NodeId != 0 && !gridIds.Contains(spc.NodeId))
                        yield return MakeError(bc.CardType, bc.Id, "G", spc.NodeId);
                    break;

                case Spc1 spc1:
                    foreach (var nodeId in spc1.NodeIds)
                    {
                        if (nodeId != 0 && !gridIds.Contains(nodeId))
                            yield return MakeError(bc.CardType, bc.Id, "Gi", nodeId);
                    }
                    break;

                case Mpc mpc:
                    foreach (var term in mpc.Terms)
                    {
                        if (term.NodeId != 0 && !gridIds.Contains(term.NodeId))
                            yield return MakeError(bc.CardType, bc.Id, "Gi", term.NodeId);
                    }
                    break;
            }
        }
    }

    private static ValidationResult MakeError(string cardType, int cardId, string field, int nodeId) =>
        new()
        {
            Severity = ValidationSeverity.Error,
            CardType = cardType,
            CardId = cardId,
            FieldName = field,
            Message = $"{cardType} {cardId}: 노드 ID {nodeId}에 해당하는 GRID가 존재하지 않습니다.",
        };
}
