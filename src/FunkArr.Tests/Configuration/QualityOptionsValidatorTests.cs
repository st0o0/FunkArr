using FunkArr.Configuration;

namespace FunkArr.Tests.Configuration;

public class QualityOptionsValidatorTests
{
    private readonly QualityOptionsValidator _sut = new();

    private static QualityOptions ValidOptions() => new()
    {
        CacheTtlMinutes = 360,
        CacheCapacity = 50000,
    };

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _sut.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_CacheTtlMinutesBelowOne_Fails()
    {
        var options = ValidOptions();
        options.CacheTtlMinutes = 0;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("CacheTtlMinutes", result.FailureMessage);
    }

    [Fact]
    public void Validate_CacheCapacityBelow100_Fails()
    {
        var options = ValidOptions();
        options.CacheCapacity = 99;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("CacheCapacity", result.FailureMessage);
    }
}
