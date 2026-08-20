using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public sealed class FunkArrOptionsValidator : IValidateOptions<FunkArrOptions>
{
    public ValidateOptionsResult Validate(string? name, FunkArrOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("FunkArr:ApiKey must be configured.");
        }

        if (options.ConcurrentDownloads < 1 || options.ConcurrentDownloads > 10)
        {
            return ValidateOptionsResult.Fail("FunkArr:ConcurrentDownloads must be between 1 and 10.");
        }

        if (string.IsNullOrWhiteSpace(options.DownloadPath))
        {
            return ValidateOptionsResult.Fail("FunkArr:DownloadPath must be configured.");
        }

        if (options.LogFormat is not ("json" or "text"))
        {
            return ValidateOptionsResult.Fail("FunkArr:LogFormat must be 'json' or 'text'.");
        }

        if (options.Postgres.IsConfigured)
        {
            var pg = options.Postgres;
            if (string.IsNullOrWhiteSpace(pg.User))
                return ValidateOptionsResult.Fail("FunkArr:Postgres:User must be configured when Postgres:Host is set.");
            if (string.IsNullOrWhiteSpace(pg.Password))
                return ValidateOptionsResult.Fail("FunkArr:Postgres:Password must be configured when Postgres:Host is set.");
        }

        if (options.QualityCacheTtlMinutes < 1)
        {
            return ValidateOptionsResult.Fail("FunkArr:QualityCacheTtlMinutes must be at least 1.");
        }

        if (options.QualityCacheCapacity < 100)
        {
            return ValidateOptionsResult.Fail("FunkArr:QualityCacheCapacity must be at least 100.");
        }

        if (options.QualityProbeLimit < 1)
        {
            return ValidateOptionsResult.Fail("FunkArr:QualityProbeLimit must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
