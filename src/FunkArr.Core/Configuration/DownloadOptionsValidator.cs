using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core;

public sealed class DownloadOptionsValidator : IValidateOptions<DownloadOptions>
{
    public ValidateOptionsResult Validate(string? name, DownloadOptions options)
    {
        var failures = new ValidationFailures();

        for (var i = 0; i < options.DownloadSchedule.Count; i++)
        {
            var slot = options.DownloadSchedule[i];
            failures.Require(slot.Start != slot.End, $"DownloadSchedule[{i}]: Start and End must differ (both are {slot.Start}).");
        }

        return failures.ToResult();
    }
}
