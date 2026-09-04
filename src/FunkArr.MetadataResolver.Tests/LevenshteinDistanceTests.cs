namespace FunkArr.MetadataResolver.Tests;

public sealed class LevenshteinDistanceTests
{
    [Fact]
    public void Identical_strings_return_one()
    {
        Assert.Equal(1.0f, LevenshteinDistance.Similarity("Roomservice", "Roomservice"));
    }

    [Fact]
    public void Both_empty_return_one()
    {
        Assert.Equal(1.0f, LevenshteinDistance.Similarity("", ""));
    }

    [Fact]
    public void One_empty_returns_zero()
    {
        Assert.Equal(0.0f, LevenshteinDistance.Similarity("test", ""));
        Assert.Equal(0.0f, LevenshteinDistance.Similarity("", "test"));
    }

    [Fact]
    public void Completely_different_returns_low_score()
    {
        var score = LevenshteinDistance.Similarity("abcdef", "zyxwvu");
        Assert.True(score < 0.3f);
    }

    [Fact]
    public void Umlaut_normalization_produces_exact_match()
    {
        Assert.Equal(1.0f, LevenshteinDistance.Similarity("Münster", "Muenster"));
    }

    [Fact]
    public void Case_insensitive()
    {
        Assert.Equal(1.0f, LevenshteinDistance.Similarity("Roomservice", "roomservice"));
    }

    [Fact]
    public void Similar_strings_return_high_score()
    {
        var score = LevenshteinDistance.Similarity("Roomservice", "Roomservice (2026)");
        Assert.True(score > 0.5f);
    }

    [Fact]
    public void Eszett_normalizes()
    {
        Assert.Equal(1.0f, LevenshteinDistance.Similarity("Straße", "Strasse"));
    }
}
