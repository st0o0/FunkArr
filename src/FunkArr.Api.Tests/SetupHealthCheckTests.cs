using System.IO.Abstractions;
using FunkArr.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkArr.Api.Tests;

public sealed class SetupHealthCheckTests
{
    [Fact]
    public void ApiKey_check_returns_ok_for_custom_key()
    {
        var options = new FunkArrOptions { ApiKey = "my-secret-key" };

        var result = SetupApiEndpoints.CheckApiKey(options);

        Assert.Equal("ok", result.Status);
        Assert.Equal("my-secret-key", result.Value);
        Assert.Equal("**********key", result.Masked);
    }

    [Fact]
    public void ApiKey_check_returns_warn_for_default_key()
    {
        var options = new FunkArrOptions();

        var result = SetupApiEndpoints.CheckApiKey(options);

        Assert.Equal("warn", result.Status);
        Assert.Contains("default", result.Message);
        Assert.Equal("funkarr-default-api-key", result.Value);
    }

    [Fact]
    public void ApiKey_mask_shows_last_three_characters()
    {
        var options = new FunkArrOptions { ApiKey = "abcdef" };

        var result = SetupApiEndpoints.CheckApiKey(options);

        Assert.Equal("***def", result.Masked);
    }

    [Fact]
    public void ApiKey_mask_handles_short_key()
    {
        var options = new FunkArrOptions { ApiKey = "ab" };

        var result = SetupApiEndpoints.CheckApiKey(options);

        Assert.Equal("ab", result.Masked);
    }

    [Fact]
    public void Directory_check_returns_ok_for_writable_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dataFiles = new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance);

        try
        {
            var result = SetupApiEndpoints.CheckDirectory(tempDir, dataFiles);

            Assert.Equal("ok", result.Status);
            Assert.Equal(Path.GetFullPath(tempDir), result.Path);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Directory_check_returns_fail_for_nonexistent_directory()
    {
        var dataFiles = new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance);

        var result = SetupApiEndpoints.CheckDirectory("/nonexistent/path/that/does/not/exist", dataFiles);

        Assert.Equal("fail", result.Status);
    }

    [Fact]
    public async Task Ffmpeg_check_returns_result()
    {
        var result = await SetupApiEndpoints.CheckFfmpeg();

        Assert.True(result.Status is "ok" or "warn");
    }
}
