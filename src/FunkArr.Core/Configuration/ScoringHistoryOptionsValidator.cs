using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class ScoringHistoryOptionsValidator : IValidateOptions<ScoringHistoryOptions>
{
    public ValidateOptionsResult Validate(string? name, ScoringHistoryOptions options)
    {
        var failures = new ValidationFailures(ScoringHistoryOptions.SectionName);

        failures.Require(options.MaxSnapshots > 0, "MaxSnapshots must be positive");
        failures.Require(options.MaxAgeDays > 0, "MaxAgeDays must be positive");
        failures.Require(options.SnapshotInterval > 0, "SnapshotInterval must be positive");

        return failures.ToResult();
    }
}
