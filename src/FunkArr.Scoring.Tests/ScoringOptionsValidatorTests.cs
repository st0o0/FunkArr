using FunkArr.Core.Configuration;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringOptionsValidatorTests
{
    private readonly ScoringOptionsValidator _validator = new();

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new ScoringOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Zero_pool_size_fails()
    {
        var options = new ScoringOptions { PoolSize = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Scoring:PoolSize must be positive", result.FailureMessage);
    }
}
