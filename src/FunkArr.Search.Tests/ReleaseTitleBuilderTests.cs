using FunkArr.Core;
using FunkArr.Messages.Scoring;

namespace FunkArr.Search.Tests;

public sealed class ReleaseTitleBuilderTests
{
    [Fact]
    public void TvWithSeasonAndEpisode()
    {
        var metadata = new MetadataSpec("01", "05", null);

        var result = ReleaseTitleBuilder.Build("Tatort", "Der letzte Schrei", metadata, 720, "tv");

        Assert.Equal("Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void TvDailyShowWithAirdateOnly()
    {
        var metadata = new MetadataSpec(null, null, new DateTimeOffset(2024, 9, 20, 0, 0, 0, TimeSpan.Zero));

        var result = ReleaseTitleBuilder.Build("heute-show", "heute-show vom 20. September 2024", metadata, 480, "tv");

        Assert.Equal("heute-show.2024-09-20.heute-show.vom.20.September.2024.GERMAN.480p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void TvWithoutMetadata()
    {
        var result = ReleaseTitleBuilder.Build("Tagesschau", "Tagesschau 20 Uhr", null, 720, "tv");

        Assert.Equal("Tagesschau.Tagesschau.20.Uhr.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void MovieWithAirdate()
    {
        var metadata = new MetadataSpec(null, null, new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero));

        var result = ReleaseTitleBuilder.Build("Der Alte", "Todfeinde", metadata, 1080, "movie");

        Assert.Equal("Der.Alte.2024.Todfeinde.GERMAN.1080p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void MovieWithoutAirdate()
    {
        var result = ReleaseTitleBuilder.Build("Polizeiruf 110", "Blutige Fährte", null, 720, "movie");

        Assert.Equal("Polizeiruf.110.Blutige.Fährte.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void UmlautsPreserved()
    {
        var result = ReleaseTitleBuilder.Build("Überführung", "Schöne Grüße", null, 720, "tv");

        Assert.Equal("Überführung.Schöne.Grüße.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void AllUmlauts()
    {
        var result = ReleaseTitleBuilder.Build("äöüÄÖÜß", "Test", null, 720, "tv");

        Assert.Equal("äöüÄÖÜß.Test.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void SpecialCharsRemoved()
    {
        var result = ReleaseTitleBuilder.Build("Show", "Title: (Special) Edition!", null, 720, "tv");

        Assert.Equal("Show.Title.Special.Edition.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void ConsecutiveDotsCollapsed()
    {
        var result = ReleaseTitleBuilder.Build("Show", "A & B", null, 720, "tv");

        Assert.Contains("Show.A.B", result);
    }

    [Fact]
    public void SingleDigitSeasonAndEpisodePadded()
    {
        var metadata = new MetadataSpec("1", "5", null);

        var result = ReleaseTitleBuilder.Build("Show", "Title", metadata, 720, "tv");

        Assert.Equal("Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void YearBasedSeason()
    {
        var metadata = new MetadataSpec("2024", "27", null);

        var result = ReleaseTitleBuilder.Build("heute-show", "Title", metadata, 480, "tv");

        Assert.Equal("heute-show.S2024E27.Title.GERMAN.480p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void AbsoluteEpisodeNumberNoSeason()
    {
        var metadata = new MetadataSpec(null, "312", null);

        var result = ReleaseTitleBuilder.Build("Löwenzahn", "Folge 312", metadata, 720, "tv");

        Assert.Equal("Löwenzahn.S01E312.Folge.312.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void QualityMapping1080()
    {
        var result = ReleaseTitleBuilder.Build("Show", "Title", null, 1080, "tv");

        Assert.Contains("1080p", result);
    }

    [Fact]
    public void QualityMapping480()
    {
        var result = ReleaseTitleBuilder.Build("Show", "Title", null, 480, "tv");

        Assert.Contains("480p", result);
    }

    [Fact]
    public void QualityMapping270()
    {
        var result = ReleaseTitleBuilder.Build("Show", "Title", null, 270, "tv");

        Assert.Contains("270p", result);
    }

    [Fact]
    public void SeasonEpisodeTakesPriorityOverAirdate()
    {
        var metadata = new MetadataSpec("01", "05", new DateTimeOffset(2024, 9, 20, 0, 0, 0, TimeSpan.Zero));

        var result = ReleaseTitleBuilder.Build("Tatort", "Title", metadata, 720, "tv");

        Assert.StartsWith("Tatort.S01E05.", result);
        Assert.DoesNotContain("2024-09-20", result);
    }

    [Fact]
    public void SzligPreserved()
    {
        var result = ReleaseTitleBuilder.Build("Straße", "Spaß", null, 720, "tv");

        Assert.Equal("Straße.Spaß.GERMAN.720p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void StripTopicFromTitle_PrefixWithColon()
    {
        var result = ReleaseTitleBuilder.StripTopicFromTitle("Tatort", "Tatort: Virus");

        Assert.Equal("Virus", result);
    }

    [Fact]
    public void StripTopicFromTitle_SuffixWithDash()
    {
        var result = ReleaseTitleBuilder.StripTopicFromTitle("Donna Leon", "Die dunkle Stunde - Donna Leon");

        Assert.Equal("Die dunkle Stunde", result);
    }

    [Fact]
    public void StripTopicFromTitle_SuffixWithEnDash()
    {
        var result = ReleaseTitleBuilder.StripTopicFromTitle("Donna Leon", "Die dunkle Stunde – Donna Leon");

        Assert.Equal("Die dunkle Stunde", result);
    }

    [Fact]
    public void StripTopicFromTitle_NoMatch()
    {
        var result = ReleaseTitleBuilder.StripTopicFromTitle("Tatort", "Mord am See");

        Assert.Equal("Mord am See", result);
    }

    [Fact]
    public void StripTopicFromTitle_WouldLeaveEmpty()
    {
        var result = ReleaseTitleBuilder.StripTopicFromTitle("Tatort", "Tatort");

        Assert.Equal("Tatort", result);
    }

    [Fact]
    public void StripTopicFromTitle_CaseInsensitive()
    {
        var result = ReleaseTitleBuilder.StripTopicFromTitle("tatort", "Tatort: Virus");

        Assert.Equal("Virus", result);
    }

    [Fact]
    public void BuildStripsTopicPrefix()
    {
        var metadata = new MetadataSpec("01", "05", null);

        var result = ReleaseTitleBuilder.Build("Tatort", "Tatort: Virus", metadata, 1080, "tv");

        Assert.Equal("Tatort.S01E05.Virus.GERMAN.1080p.WEB.h264-FunkArr", result);
    }

    [Fact]
    public void BuildStripsTopicSuffix()
    {
        var result = ReleaseTitleBuilder.Build("Donna Leon", "Die dunkle Stunde - Donna Leon", null, 1080, "movie");

        Assert.Equal("Donna.Leon.Die.dunkle.Stunde.GERMAN.1080p.WEB.h264-FunkArr", result);
    }
}
