using FunkArr.Configuration;

namespace FunkArr.Tests.Configuration;

public class SearchOptionsValidatorTests
{
    private readonly SearchOptionsValidator _sut = new();

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _sut.Validate(null, new SearchOptions { QualityProbeLimit = 30 });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_QualityProbeLimitBelowOne_Fails()
    {
        var result = _sut.Validate(null, new SearchOptions { QualityProbeLimit = 0 });

        Assert.True(result.Failed);
        Assert.Contains("QualityProbeLimit", result.FailureMessage);
    }
}
