using FunkArr.Core.Configuration;

namespace FunkArr.Download.Tests;

public sealed class FunkArrOptionsValidatorTests
{
    private readonly FunkArrOptionsValidator _validator = new();

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new FunkArrOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Empty_api_key_fails()
    {
        var options = new FunkArrOptions { ApiKey = "" };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:ApiKey must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Whitespace_api_key_fails()
    {
        var options = new FunkArrOptions { ApiKey = "  " };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:ApiKey must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Empty_data_path_fails()
    {
        var options = new FunkArrOptions { DataPath = "" };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:DataPath must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Multiple_failures_reported()
    {
        var options = new FunkArrOptions { ApiKey = "", DataPath = "" };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ApiKey", result.FailureMessage);
        Assert.Contains("DataPath", result.FailureMessage);
    }
}
