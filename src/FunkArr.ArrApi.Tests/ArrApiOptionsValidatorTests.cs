using FunkArr.ArrApi;

namespace FunkArr.ArrApi.Tests;

public sealed class ArrApiOptionsValidatorTests
{
    private readonly ArrApiOptionsValidator _validator = new();

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new ArrApiOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Negative_cache_ttl_fails()
    {
        var options = new ArrApiOptions { SearchCacheTtlSeconds = -1 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:ArrApi:SearchCacheTtlSeconds must not be negative", result.FailureMessage);
    }

    [Fact]
    public void Zero_cache_ttl_passes()
    {
        var options = new ArrApiOptions { SearchCacheTtlSeconds = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
