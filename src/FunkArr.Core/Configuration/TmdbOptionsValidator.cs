using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class TmdbOptionsValidator : IValidateOptions<TmdbOptions>
{
    public ValidateOptionsResult Validate(string? name, TmdbOptions options)
    {
        var failures = new ValidationFailures(TmdbOptions.SectionName);

        failures.Require(!string.IsNullOrWhiteSpace(options.ApiKey), "ApiKey must not be empty");

        return failures.ToResult();
    }
}
