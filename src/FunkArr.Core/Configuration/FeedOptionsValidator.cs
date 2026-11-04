using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core;

public sealed class FeedOptionsValidator : IValidateOptions<FeedOptions>
{
    public ValidateOptionsResult Validate(string? name, FeedOptions options)
    {
        var failures = new ValidationFailures(FeedOptions.SectionName);

        failures.Require(options.Interval > TimeSpan.Zero, "Interval must be positive");
        failures.Require(options.PageSize is > 0 and <= 1000, "PageSize must be between 1 and 1000");
        failures.Require(options.MaxPages > 0, "MaxPages must be positive");
        failures.Require(options.MaxCatchUp > TimeSpan.Zero, "MaxCatchUp must be positive");
        failures.Require(options.MaxConcurrency > 0, "MaxConcurrency must be positive");
        failures.Require(options.MinConfidence is >= 0 and <= 1, "MinConfidence must be between 0 and 1");
        failures.Require(options.Retention > TimeSpan.Zero, "Retention must be positive");
        failures.Require(options.InitialLookback > TimeSpan.Zero, "InitialLookback must be positive");

        return failures.ToResult();
    }
}
