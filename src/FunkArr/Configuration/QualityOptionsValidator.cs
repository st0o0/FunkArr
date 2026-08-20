using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public sealed class QualityOptionsValidator : IValidateOptions<QualityOptions>
{
    public ValidateOptionsResult Validate(string? name, QualityOptions options)
    {
        if (options.CacheTtlMinutes < 1)
        {
            return ValidateOptionsResult.Fail("FunkArr:Quality:CacheTtlMinutes must be at least 1.");
        }

        if (options.CacheCapacity < 100)
        {
            return ValidateOptionsResult.Fail("FunkArr:Quality:CacheCapacity must be at least 100.");
        }

        return ValidateOptionsResult.Success;
    }
}
