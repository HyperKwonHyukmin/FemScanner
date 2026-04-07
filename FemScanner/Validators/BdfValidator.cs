using FemScanner.Models;
using FemScanner.Validators.Rules;

namespace FemScanner.Validators;

/// <summary>
/// BDF 모델 검증 오케스트레이터.
/// 등록된 규칙들을 순회하며 ValidationResult를 수집합니다.
/// </summary>
public class BdfValidator
{
    private readonly List<IValidationRule> _rules;

    public BdfValidator()
    {
        _rules =
        [
            new GridRule(),
            new ElementRule(),
            new PropertyRule(),
            new MaterialRule(),
            new LoadRule(),
            new BcRule(),
        ];
    }

    /// <summary>등록된 검증 규칙 이름 목록</summary>
    public IReadOnlyList<string> RuleNames => _rules.Select(r => r.GetType().Name).ToList();

    /// <summary>BdfModel에 대해 모든 검증 규칙을 실행하고 결과 목록을 반환합니다.</summary>
    public IReadOnlyList<ValidationResult> Validate(BdfModel model)
    {
        return _rules.SelectMany(r => r.Validate(model)).ToList();
    }

    /// <summary>
    /// 규칙별 상세 결과(RuleCheckResult)와 전체 ValidationResult를 함께 반환합니다.
    /// </summary>
    public (IReadOnlyList<ValidationResult> Results, IReadOnlyList<RuleCheckResult> RuleChecks)
        ValidateDetailed(BdfModel model)
    {
        var allResults = new List<ValidationResult>();
        var ruleChecks = new List<RuleCheckResult>();

        foreach (var rule in _rules)
        {
            var ruleResults = rule.Validate(model).ToList();
            int errors   = ruleResults.Count(r => r.Severity == ValidationSeverity.Error);
            int warnings = ruleResults.Count(r => r.Severity == ValidationSeverity.Warning);
            int checkedCount = GetCheckedCount(rule, model);

            string status = errors > 0 ? "error" : warnings > 0 ? "warning" : "pass";
            ruleChecks.Add(new RuleCheckResult
            {
                Rule         = rule.GetType().Name,
                Status       = status,
                CheckedCount = checkedCount,
                ErrorCount   = errors,
                WarningCount = warnings,
            });

            allResults.AddRange(ruleResults);
        }

        return (allResults, ruleChecks);
    }

    /// <summary>규칙이 검사하는 대상 카드 수를 반환합니다.</summary>
    private static int GetCheckedCount(IValidationRule rule, BdfModel model) => rule switch
    {
        GridRule     => model.Grids.Count,
        ElementRule  => model.Elements.Count,
        PropertyRule => model.Properties.Count,
        MaterialRule => model.Materials.Count,
        LoadRule     => model.Loads.Count,
        BcRule       => model.BoundaryConditions.Count,
        _            => 0,
    };
}
