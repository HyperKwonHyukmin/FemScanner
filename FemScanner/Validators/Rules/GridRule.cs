using FemScanner.Models;

namespace FemScanner.Validators.Rules;

/// <summary>GRID 카드 검증 규칙</summary>
public class GridRule : IValidationRule
{
    public IEnumerable<ValidationResult> Validate(BdfModel model)
    {
        var seenIds = new HashSet<int>();

        foreach (var grid in model.Grids)
        {
            // 중복 ID 검사
            if (!seenIds.Add(grid.Id))
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    CardType = "GRID",
                    CardId = grid.Id,
                    FieldName = "ID",
                    Message = $"GRID ID {grid.Id}가 중복 정의되었습니다.",
                };
            }

            // ID 유효성 (양수)
            if (grid.Id <= 0)
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    CardType = "GRID",
                    CardId = grid.Id,
                    FieldName = "ID",
                    Message = $"GRID ID는 양수여야 합니다. (현재값: {grid.Id})",
                };
            }
        }
    }
}
