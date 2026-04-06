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

    /// <summary>BdfModel에 대해 모든 검증 규칙을 실행하고 결과 목록을 반환합니다.</summary>
    public IReadOnlyList<ValidationResult> Validate(BdfModel model)
    {
        return _rules.SelectMany(r => r.Validate(model)).ToList();
    }
}
