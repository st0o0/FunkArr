using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class TvdbOptionsValidator : IValidateOptions<TvdbOptions>
{
    public ValidateOptionsResult Validate(string? name, TvdbOptions options)
    {
        var failures = new ValidationFailures(TvdbOptions.SectionName);

        failures.Require(!string.IsNullOrWhiteSpace(options.ApiKey), "ApiKey must not be empty");

        return failures.ToResult();
    }
}
