using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet;

public interface IRuleSetValidator
{
    IReadOnlyList<RuleSetValidationError> Validate(string json);
}
