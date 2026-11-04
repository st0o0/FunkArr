using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class ScoringOptionsValidator : IValidateOptions<ScoringOptions>
{
    public ValidateOptionsResult Validate(string? name, ScoringOptions options)
    {
        var failures = new ValidationFailures(ScoringOptions.SectionName);

        failures.Require(options.PoolSize > 0, "PoolSize must be positive");

        return failures.ToResult();
    }
}
