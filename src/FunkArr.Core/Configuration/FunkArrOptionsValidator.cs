using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class FunkArrOptionsValidator : IValidateOptions<FunkArrOptions>
{
    public ValidateOptionsResult Validate(string? name, FunkArrOptions options)
    {
        var failures = new ValidationFailures(FunkArrOptions.SectionName);

        failures.Require(!string.IsNullOrWhiteSpace(options.ApiKey), "ApiKey must not be empty");
        failures.Require(!string.IsNullOrWhiteSpace(options.DataPath), "DataPath must not be empty");

        return failures.ToResult();
    }
}
