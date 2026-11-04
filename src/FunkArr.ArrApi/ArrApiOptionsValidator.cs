using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.ArrApi;

public sealed class ArrApiOptionsValidator : IValidateOptions<ArrApiOptions>
{
    public ValidateOptionsResult Validate(string? name, ArrApiOptions options)
    {
        var failures = new ValidationFailures(ArrApiOptions.SectionName);

        failures.Require(options.SearchCacheTtlSeconds >= 0, "SearchCacheTtlSeconds must not be negative");

        return failures.ToResult();
    }
}
