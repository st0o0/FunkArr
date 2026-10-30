using FunkArr.Core;
using FunkArr.Messages;

namespace FunkArr.Search.Tests;

public sealed class VariantOptionsTests
{
    [Theory]
    [InlineData(MediathekVariant.AudioDescription)]
    [InlineData(MediathekVariant.SignLanguage)]
    [InlineData(MediathekVariant.PlainLanguage)]
    [InlineData(MediathekVariant.OriginalVersion)]
    public void Defaults_exclude_every_non_standard_variant(MediathekVariant variant)
    {
        var options = new VariantOptions();

        Assert.Multiple(
            () => Assert.Equal(VariantPolicy.Exclude, options.PolicyFor(variant)),
            () => Assert.True(options.IsExcluded(variant)));
    }

    [Fact]
    public void Standard_is_never_excluded()
    {
        var options = new VariantOptions();

        Assert.Multiple(
            () => Assert.Equal(VariantPolicy.Allow, options.PolicyFor(MediathekVariant.Standard)),
            () => Assert.False(options.IsExcluded(MediathekVariant.Standard)));
    }

    [Fact]
    public void Allowing_one_variant_keeps_the_others_excluded()
    {
        var options = new VariantOptions { AudioDescription = VariantPolicy.Allow };

        Assert.Multiple(
            () => Assert.False(options.IsExcluded(MediathekVariant.AudioDescription)),
            () => Assert.True(options.IsExcluded(MediathekVariant.SignLanguage)),
            () => Assert.True(options.IsExcluded(MediathekVariant.PlainLanguage)),
            () => Assert.True(options.IsExcluded(MediathekVariant.OriginalVersion)));
    }
}
