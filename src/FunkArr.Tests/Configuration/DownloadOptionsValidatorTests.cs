using FunkArr.Configuration;

namespace FunkArr.Tests.Configuration;

public class DownloadOptionsValidatorTests
{
    private readonly DownloadOptionsValidator _sut = new();

    private static DownloadOptions ValidOptions() => new()
    {
        DownloadPath = "/media/downloads",
        ConcurrentDownloads = 3,
    };

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _sut.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyDownloadPath_Fails()
    {
        var options = ValidOptions();
        options.DownloadPath = "";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DownloadPath", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Validate_ConcurrentDownloadsOutOfRange_Fails(int value)
    {
        var options = ValidOptions();
        options.ConcurrentDownloads = value;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ConcurrentDownloads", result.FailureMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Validate_ConcurrentDownloadsAtBounds_Succeeds(int value)
    {
        var options = ValidOptions();
        options.ConcurrentDownloads = value;

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
