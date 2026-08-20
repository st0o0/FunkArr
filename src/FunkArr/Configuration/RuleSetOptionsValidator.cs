using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public sealed class RuleSetOptionsValidator : IValidateOptions<RuleSetOptions>
{
    public ValidateOptionsResult Validate(string? name, RuleSetOptions options) =>
        ValidateOptionsResult.Success;
}
