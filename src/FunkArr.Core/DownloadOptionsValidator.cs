using Microsoft.Extensions.Options;

namespace FunkArr.Core;

public sealed class DownloadOptionsValidator : IValidateOptions<DownloadOptions>
{
    public ValidateOptionsResult Validate(string? name, DownloadOptions options)
    {
        var failures = new List<string>();

        if (options.SpeedLimitBytesPerSecond is < 0)
        {
            failures.Add("SpeedLimitBytesPerSecond must be non-negative.");
        }

        for (var i = 0; i < options.DownloadSchedule.Count; i++)
        {
            var slot = options.DownloadSchedule[i];
            if (slot.Start == slot.End)
            {
                failures.Add($"DownloadSchedule[{i}]: Start and End must differ (both are {slot.Start}).");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
