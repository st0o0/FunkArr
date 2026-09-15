namespace FunkArr.Core;

public sealed record RuleSetValidationError(string Field, string Message);

public interface IRuleSetValidator
{
    IReadOnlyList<RuleSetValidationError> Validate(string json);
}
