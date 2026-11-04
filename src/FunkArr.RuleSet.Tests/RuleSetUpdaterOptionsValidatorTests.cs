using FunkArr.Core;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetUpdaterOptionsValidatorTests
{
    private readonly RuleSetUpdaterOptionsValidator _validator = new();

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new RuleSetUpdaterOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Empty_repository_fails()
    {
        var options = new RuleSetUpdaterOptions { Repository = "" };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:RuleSet:Repository must not be empty", result.FailureMessage);
    }
}
