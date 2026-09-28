using FunkArr.Messages.RuleSet;

namespace FunkArr.Core;

public interface IRuleSetValidator
{
    IReadOnlyList<RuleSetValidationError> Validate(string json);
}
