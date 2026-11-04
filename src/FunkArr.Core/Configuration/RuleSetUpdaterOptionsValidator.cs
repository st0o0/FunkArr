using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class RuleSetUpdaterOptionsValidator : IValidateOptions<RuleSetUpdaterOptions>
{
    public ValidateOptionsResult Validate(string? name, RuleSetUpdaterOptions options)
    {
        var failures = new ValidationFailures(RuleSetUpdaterOptions.SectionName);

        failures.Require(!string.IsNullOrWhiteSpace(options.Repository), "Repository must not be empty");

        return failures.ToResult();
    }
}
