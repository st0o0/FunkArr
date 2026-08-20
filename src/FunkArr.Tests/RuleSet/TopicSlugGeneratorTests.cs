using FunkArr.RuleSet;

namespace FunkArr.Tests.RuleSet;

public class TopicSlugGeneratorTests
{
    [Theory]
    [InlineData("Feuer & Flamme", "feuer-und-flamme")]
    [InlineData("heute-show", "heute-show")]
    [InlineData("ZDF Magazin Royale", "zdf-magazin-royale")]
    [InlineData("Tatort", "tatort")]
    [InlineData("Löwenzähn", "loewenzaehn")]
    [InlineData("Große Straße", "grosse-strasse")]
    [InlineData("Müller-Lüdenscheid", "mueller-luedenscheid")]
    [InlineData("Sturm der Liebe", "sturm-der-liebe")]
    [InlineData("Die Sendung mit der Maus", "die-sendung-mit-der-maus")]
    [InlineData("Checker Can, Checker Tobi und Checker Julian", "checker-can-checker-tobi-und-checker-julian")]
    [InlineData("Terra X", "terra-x")]
    [InlineData("Tatort Schimanski | restauriert in HD", "tatort-schimanski-restauriert-in-hd")]
    public void Generate_ProducesExpectedSlug(string topic, string expected)
    {
        var result = TopicSlugGenerator.Generate(topic);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  Spaces  Around  ", "spaces-around")]
    [InlineData("---hyphens---", "hyphens")]
    [InlineData("a & b & c", "a-und-b-und-c")]
    public void Generate_HandlesEdgeCases(string topic, string expected)
    {
        var result = TopicSlugGenerator.Generate(topic);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Generate_ReturnsUnknown_ForEmptyInput()
    {
        Assert.Equal("unknown", TopicSlugGenerator.Generate(""));
        Assert.Equal("unknown", TopicSlugGenerator.Generate("   "));
        Assert.Equal("unknown", TopicSlugGenerator.Generate("---"));
    }
}
