using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public sealed class DownloadOptionsValidator : IValidateOptions<DownloadOptions>
{
    public ValidateOptionsResult Validate(string? name, DownloadOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Path))
        {
            return ValidateOptionsResult.Fail("FunkArr:Download:Path must be configured.");
        }

        if (options.ConcurrentDownloads < 1 || options.ConcurrentDownloads > 10)
        {
            return ValidateOptionsResult.Fail("FunkArr:Download:ConcurrentDownloads must be between 1 and 10.");
        }

        return ValidateOptionsResult.Success;
    }
}
