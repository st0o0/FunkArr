using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public sealed class SearchOptionsValidator : IValidateOptions<SearchOptions>
{
    public ValidateOptionsResult Validate(string? name, SearchOptions options)
    {
        if (options.QualityProbeLimit < 1)
        {
            return ValidateOptionsResult.Fail("FunkArr:Search:QualityProbeLimit must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
