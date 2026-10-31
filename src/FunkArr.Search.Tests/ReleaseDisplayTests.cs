namespace FunkArr.Search.Tests;

public sealed class ReleaseDisplayTests
{
    [Fact]
    public void From_StripsPrefixWithColon()
    {
        var display = ReleaseDisplay.From("Tatort: Virus", "Tatort");

        Assert.Equal("Tatort", display.MediaName);
        Assert.Equal("Virus", display.EpisodeTitle);
    }

    [Fact]
    public void From_StripsPrefixWithDash()
    {
        var display = ReleaseDisplay.From("Donna Leon - Die dunkle Stunde", "Donna Leon");

        Assert.Equal("Die dunkle Stunde", display.EpisodeTitle);
    }

    [Fact]
    public void From_StripsSuffix()
    {
        var display = ReleaseDisplay.From("Die dunkle Stunde - Donna Leon", "Donna Leon");

        Assert.Equal("Die dunkle Stunde", display.EpisodeTitle);
    }

    [Fact]
    public void From_StripsSuffixWithEnDash()
    {
        var display = ReleaseDisplay.From("Die dunkle Stunde – Donna Leon", "Donna Leon");

        Assert.Equal("Die dunkle Stunde", display.EpisodeTitle);
    }

    [Fact]
    public void From_StripsSeasonEpisodePattern()
    {
        var display = ReleaseDisplay.From("Test (S01/E05)", "Show");

        Assert.Equal("Test", display.EpisodeTitle);
    }

    [Fact]
    public void From_CaseInsensitive()
    {
        var display = ReleaseDisplay.From("tatort: Virus", "Tatort");

        Assert.Equal("Virus", display.EpisodeTitle);
    }

    [Fact]
    public void From_NoMatch_ReturnsAsIs()
    {
        var display = ReleaseDisplay.From("Something Completely Different", "Tatort");

        Assert.Equal("Something Completely Different", display.EpisodeTitle);
    }

    [Fact]
    public void From_EmptyMediaName_ReturnsAsIs()
    {
        var display = ReleaseDisplay.From("Some Title", "");

        Assert.Equal("Some Title", display.EpisodeTitle);
    }

    [Fact]
    public void From_PrefixStrippedButEmptyResult_KeepsOriginal()
    {
        var display = ReleaseDisplay.From("Tatort ", "Tatort");

        Assert.Equal("Tatort", display.MediaName);
    }
}
