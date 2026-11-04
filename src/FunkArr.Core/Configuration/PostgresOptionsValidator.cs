using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class PostgresOptionsValidator : IValidateOptions<PostgresOptions>
{
    public ValidateOptionsResult Validate(string? name, PostgresOptions options)
    {
        var failures = new ValidationFailures(PostgresOptions.SectionName);

        failures.Require(options.Port is > 0 and <= 65535, "Port must be between 1 and 65535");
        failures.Require(!string.IsNullOrWhiteSpace(options.Database), "Database must not be empty");

        return failures.ToResult();
    }
}
