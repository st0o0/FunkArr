using FunkArr.Core;

namespace FunkArr.Download.Tests;

public sealed class DownloadOptionsValidatorTests
{
    private readonly DownloadOptionsValidator _validator = new();

    [Fact]
    public void Valid_config_passes()
    {
        var options = new DownloadOptions
        {
            DownloadSchedule =
            [
                new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
            ]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new DownloadOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Zero_length_slot_fails()
    {
        var options = new DownloadOptions
        {
            DownloadSchedule =
            [
                new() { Start = new TimeOnly(23, 0), End = new TimeOnly(23, 0) }
            ]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Start and End must differ", result.FailureMessage);
    }

}
