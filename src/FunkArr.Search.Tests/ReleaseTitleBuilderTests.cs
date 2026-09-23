using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Search;

namespace FunkArr.Search.Tests;

public sealed class ReleaseTitleBuilderTests
{
    [Fact]
    public void TvWithSeasonAndEpisode()
    {
        var result = ReleaseTitleBuilder.Format("Tatort", "S01E05", "Der letzte Schrei", 720);

        Assert.Equal("Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void TvDailyShowWithAirdate()
    {
        var result = ReleaseTitleBuilder.Format("heute-show", "2024-09-20", "vom 20. September 2024", 480);

        Assert.Equal("heute-show.2024-09-20.vom.20.September.2024.GERMAN.480p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void TvWithoutIdentifier()
    {
        var result = ReleaseTitleBuilder.Format("Tagesschau", null, "20 Uhr", 720);

        Assert.Equal("Tagesschau.20.Uhr.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void MovieWithYear()
    {
        var result = ReleaseTitleBuilder.Format("Der Alte", "2024", "Todfeinde", 1080);

        Assert.Equal("Der.Alte.2024.Todfeinde.GERMAN.1080p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void MovieWithoutYear()
    {
        var result = ReleaseTitleBuilder.Format("Polizeiruf 110", null, "Blutige Fährte", 720);

        Assert.Equal("Polizeiruf.110.Blutige.Fährte.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void UmlautsPreserved()
    {
        var result = ReleaseTitleBuilder.Format("Überführung", null, "Schöne Grüße", 720);

        Assert.Equal("Überführung.Schöne.Grüße.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void SzligPreserved()
    {
        var result = ReleaseTitleBuilder.Format("Straße", null, "Spaß", 720);

        Assert.Equal("Straße.Spaß.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void SpecialCharsRemovedFromEpisodeTitle()
    {
        var result = ReleaseTitleBuilder.Format("Show", "S01E01", "Title: (Special) Edition!", 720);

        Assert.Equal("Show.S01E01.Title.Special.Edition.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void ConsecutiveDotsCollapsed()
    {
        var result = ReleaseTitleBuilder.Format("Show", null, "A & B", 720);

        Assert.Contains("Show.A.B", result);
    }

    [Fact]
    public void QualityMapping1080()
    {
        var result = ReleaseTitleBuilder.Format("Show", null, "Title", 1080);

        Assert.Contains("1080p", result);
    }

    [Fact]
    public void QualityMapping480()
    {
        var result = ReleaseTitleBuilder.Format("Show", null, "Title", 480);

        Assert.Contains("480p", result);
    }

    [Fact]
    public void QualityMapping270()
    {
        var result = ReleaseTitleBuilder.Format("Show", null, "Title", 270);

        Assert.Contains("270p", result);
    }

    [Fact]
    public void FormatSeasonEpisodePadsSingleDigits()
    {
        var result = ReleaseTitleBuilder.FormatSeasonEpisode("1", "5");

        Assert.Equal("S01E05", result);
    }

    [Fact]
    public void FormatSeasonEpisodePreservesYearBased()
    {
        var result = ReleaseTitleBuilder.FormatSeasonEpisode("2024", "27");

        Assert.Equal("S2024E27", result);
    }

    [Fact]
    public void IdentifierNotDuplicatedInTitle()
    {
        var result = ReleaseTitleBuilder.Format("Leschs Kosmos", "S2026E10", "Jahrhunderthitze", 1080);

        Assert.Equal("Leschs.Kosmos.S2026E10.Jahrhunderthitze.GERMAN.1080p.WEB.h264-FunkArr", result);
        Assert.Equal(1, CountOccurrences(result, "S2026E10"));
    }

    [Fact]
    public void MediaNameNotDuplicatedInTitle()
    {
        var result = ReleaseTitleBuilder.Format("Lieselotte", "S01E30", "hat den Dreh raus", 1080);

        Assert.Equal("Lieselotte.S01E30.hat.den.Dreh.raus.GERMAN.1080p.WEB.h264-FunkArr", result);
        Assert.Equal(1, CountOccurrences(result, "Lieselotte"));
    }

    [Fact]
    public void CleanTitle_PrefixWithColon()
    {
        var result = ReleaseVariant.CleanTitle("Tatort: Virus", "Tatort");

        Assert.Equal("Virus", result);
    }

    [Fact]
    public void CleanTitle_SuffixWithDash()
    {
        var result = ReleaseVariant.CleanTitle("Die dunkle Stunde - Donna Leon", "Donna Leon");

        Assert.Equal("Die dunkle Stunde", result);
    }

    [Fact]
    public void CleanTitle_SuffixWithEnDash()
    {
        var result = ReleaseVariant.CleanTitle("Die dunkle Stunde – Donna Leon", "Donna Leon");

        Assert.Equal("Die dunkle Stunde", result);
    }

    [Fact]
    public void CleanTitle_PrefixWithSpace()
    {
        var result = ReleaseVariant.CleanTitle("Lieselotte hat den Dreh raus", "Lieselotte");

        Assert.Equal("hat den Dreh raus", result);
    }

    [Fact]
    public void CleanTitle_PrefixAndSuffix()
    {
        var result = ReleaseVariant.CleanTitle(
            "ZDF Magazin Royale Zehn Jahre Ehrenfeld - ZDF Magazin Royale",
            "ZDF Magazin Royale");

        Assert.Equal("Zehn Jahre Ehrenfeld", result);
    }

    [Fact]
    public void CleanTitle_NoMatch()
    {
        var result = ReleaseVariant.CleanTitle("Mord am See", "Tatort");

        Assert.Equal("Mord am See", result);
    }

    [Fact]
    public void CleanTitle_CaseInsensitive()
    {
        var result = ReleaseVariant.CleanTitle("Tatort: Virus", "tatort");

        Assert.Equal("Virus", result);
    }

    [Fact]
    public void BuildIdentifier_SeasonAndEpisode()
    {
        var identity = new MediaIdentity(null, null, null, "2026", "17");
        var result = ReleaseVariant.BuildIdentifier(identity, null, MediaType.Show);

        Assert.Equal("S2026E17", result);
    }

    [Fact]
    public void BuildIdentifier_EpisodeOnly()
    {
        var identity = new MediaIdentity(null, null, null, null, "312");
        var result = ReleaseVariant.BuildIdentifier(identity, null, MediaType.Show);

        Assert.Equal("S01E312", result);
    }

    [Fact]
    public void BuildIdentifier_AirdateForShow()
    {
        var identity = new MediaIdentity(null, null, null, null, null);
        var airedAt = new DateTimeOffset(2024, 9, 20, 0, 0, 0, TimeSpan.Zero);
        var result = ReleaseVariant.BuildIdentifier(identity, airedAt, MediaType.Show);

        Assert.Equal("2024-09-20", result);
    }

    [Fact]
    public void BuildIdentifier_YearForMovie()
    {
        var identity = new MediaIdentity(null, null, null, null, null);
        var airedAt = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero);
        var result = ReleaseVariant.BuildIdentifier(identity, airedAt, MediaType.Movie);

        Assert.Equal("2024", result);
    }

    [Fact]
    public void BuildIdentifier_NullWhenNoData()
    {
        var identity = new MediaIdentity(null, null, null, null, null);
        var result = ReleaseVariant.BuildIdentifier(identity, null, MediaType.Show);

        Assert.Null(result);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }

        return count;
    }
}
