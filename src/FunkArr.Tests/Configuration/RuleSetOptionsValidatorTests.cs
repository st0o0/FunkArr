using FunkArr.Configuration;

namespace FunkArr.Tests.Configuration;

public class RuleSetOptionsValidatorTests
{
    private readonly RuleSetOptionsValidator _sut = new();

    [Fact]
    public void Validate_AnyOptions_Succeeds()
    {
        var result = _sut.Validate(null, new RuleSetOptions());

        Assert.True(result.Succeeded);
    }
}
