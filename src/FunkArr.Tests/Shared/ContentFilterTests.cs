using FunkArr.Shared;

namespace FunkArr.Tests.Shared;

public class ContentFilterTests
{
    [Theory]
    [InlineData("Tatort (Audiodeskription)")]
    [InlineData("Tagesschau in Gebärdensprache")]
    [InlineData("Tagesschau in Gebardensprache")]
    [InlineData("Nachrichten in klare Sprache")]
    [InlineData("Hörfilm - Hörfassung")]
    public void IsAccessibilityVariant_ReturnsTrue_ForAccessibilityKeywords(string title)
    {
        Assert.True(ContentFilter.IsAccessibilityVariant(title));
    }

    [Theory]
    [InlineData("Tatort: Ein Mord zuviel")]
    [InlineData("Tagesschau 20 Uhr")]
    [InlineData("Tatort - Trailer")]
    public void IsAccessibilityVariant_ReturnsFalse_ForNonAccessibilityTitles(string title)
    {
        Assert.False(ContentFilter.IsAccessibilityVariant(title));
    }

    [Theory]
    [InlineData("tatort audiodeskription")]
    [InlineData("TATORT GEBÄRDENSPRACHE")]
    [InlineData("Nachrichten In Klare Sprache")]
    public void IsAccessibilityVariant_IsCaseInsensitive(string title)
    {
        Assert.True(ContentFilter.IsAccessibilityVariant(title));
    }

    [Theory]
    [InlineData("Tatort (Audiodeskription)")]
    [InlineData("Tagesschau in Gebärdensprache")]
    public void ShouldSkipAccessibilityOnly_ReturnsTrue_ForAccessibilityKeywords(string title)
    {
        Assert.True(ContentFilter.ShouldSkipAccessibilityOnly(title));
    }

    [Theory]
    [InlineData("Tatort - Trailer")]
    [InlineData("Vorschau auf die nächste Folge")]
    [InlineData("Teaser")]
    public void ShouldSkipAccessibilityOnly_ReturnsFalse_ForContentTypeKeywords(string title)
    {
        Assert.False(ContentFilter.ShouldSkipAccessibilityOnly(title));
    }

    [Theory]
    [InlineData("Tatort (Audiodeskription)", "Tatort")]
    [InlineData("Tatort - Trailer", "Tatort")]
    [InlineData("Vorschau auf die nächste Folge", "Tatort")]
    [InlineData("Folge 12", "Teaser")]
    [InlineData("Folge 12", "Vorschau")]
    [InlineData("Nachrichten in klare Sprache", "Tagesschau")]
    [InlineData("Hörfilm - Hörfassung", "Tatort")]
    [InlineData("Gebardensprache Version", "Tagesschau")]
    public void ShouldSkip_ReturnsTrue_ForAnyMatchingKeyword(string title, string topic)
    {
        Assert.True(ContentFilter.ShouldSkip(title, topic));
    }

    [Fact]
    public void ShouldSkip_ReturnsFalse_WhenNoKeywordPresent()
    {
        Assert.False(ContentFilter.ShouldSkip("Folge 12", "Tatort"));
    }

    [Fact]
    public void ShouldSkip_ChecksContentTypeKeywords_InTopicOnly()
    {
        Assert.True(ContentFilter.ShouldSkip("Folge 12", "Vorschau"));
    }

    [Fact]
    public void ShouldSkip_DoesNotCheckAccessibilityKeywords_InTopic()
    {
        Assert.False(ContentFilter.ShouldSkip("Folge 12", "Audiodeskription"));
    }

    [Theory]
    [InlineData("TATORT TRAILER", "tatort")]
    [InlineData("tatort trailer", "TATORT")]
    public void ShouldSkip_IsCaseInsensitive(string title, string topic)
    {
        Assert.True(ContentFilter.ShouldSkip(title, topic));
    }
}
