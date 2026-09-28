using FunkArr.Messages.Enrichment;

namespace FunkArr.Search.Tests;

public sealed class SceneReleaseTests
{
    // --- ForShow identifier resolution ---

    [Fact]
    public void ForShow_SeasonAndEpisode()
    {
        var item = MakeItem(season: "2026", episode: "18");

        var release = SceneRelease.ForShow(item, "Tatort");

        Assert.Equal("S2026E18", release.Identifier);
    }

    [Fact]
    public void ForShow_EpisodeOnly()
    {
        var item = MakeItem(episode: "5");

        var release = SceneRelease.ForShow(item, "Tatort");

        Assert.Equal("S01E05", release.Identifier);
    }

    [Fact]
    public void ForShow_EpisodeOnlyMultiDigit()
    {
        var item = MakeItem(episode: "312");

        var release = SceneRelease.ForShow(item, "Tatort");

        Assert.Equal("S01E312", release.Identifier);
    }

    [Fact]
    public void ForShow_AirdateFallback()
    {
        var item = MakeItem(airedAt: new DateTimeOffset(2024, 9, 20, 0, 0, 0, TimeSpan.Zero));

        var release = SceneRelease.ForShow(item, "Tatort");

        Assert.Equal("2024-09-20", release.Identifier);
    }

    [Fact]
    public void ForShow_NoData_NullIdentifier()
    {
        var item = MakeItem();

        var release = SceneRelease.ForShow(item, "Tatort");

        Assert.Null(release.Identifier);
    }

    [Fact]
    public void ForShow_UsesDisplayFromItem()
    {
        var item = MakeItem(season: "1", episode: "3",
            display: new ReleaseDisplay("Leschs Kosmos", "Jahrhunderthitze"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Equal("Leschs Kosmos", release.MediaName);
        Assert.Equal("Jahrhunderthitze", release.EpisodeTitle);
    }

    [Fact]
    public void ForShow_FallbacksToCleanTitleWhenNoDisplay()
    {
        var item = MakeItem(title: "Tatort: Herz aus Eis", topic: "Tatort", display: null);

        var release = SceneRelease.ForShow(item, "Tatort");

        Assert.Equal("Tatort", release.MediaName);
        Assert.Equal("Herz aus Eis", release.EpisodeTitle);
    }

    // --- ForMovie identifier resolution ---

    [Fact]
    public void ForMovie_EnrichedYear()
    {
        var item = MakeItem(year: 1995, airedAt: new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero));

        var release = SceneRelease.ForMovie(item, "The Usual Suspects");

        Assert.Equal("1995", release.Identifier);
    }

    [Fact]
    public void ForMovie_FallbackToBroadcastYear()
    {
        var item = MakeItem(airedAt: new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero));

        var release = SceneRelease.ForMovie(item, "Der Alte");

        Assert.Equal("2024", release.Identifier);
    }

    [Fact]
    public void ForMovie_NoYear_NullIdentifier()
    {
        var item = MakeItem();

        var release = SceneRelease.ForMovie(item, "Film");

        Assert.Null(release.Identifier);
    }

    // --- FormatTitle ---

    [Fact]
    public void FormatTitle_TvWithSeasonAndEpisode()
    {
        var item = MakeItem(season: "01", episode: "05",
            display: new ReleaseDisplay("Tatort", "Der letzte Schrei"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Equal("Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr", release.FormatTitle(720));
    }

    [Fact]
    public void FormatTitle_TvDailyShowWithAirdate()
    {
        var item = MakeItem(airedAt: new DateTimeOffset(2024, 9, 20, 0, 0, 0, TimeSpan.Zero),
            display: new ReleaseDisplay("heute-show", "vom 20. September 2024"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Equal("heute-show.2024-09-20.vom.20.September.2024.GERMAN.480p.WEB.h264-FunkArr", release.FormatTitle(480));
    }

    [Fact]
    public void FormatTitle_NoIdentifier()
    {
        var item = MakeItem(display: new ReleaseDisplay("Tagesschau", "20 Uhr"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Equal("Tagesschau.20.Uhr.GERMAN.720p.WEB.h264-FunkArr", release.FormatTitle(720));
    }

    [Fact]
    public void FormatTitle_MovieWithYear()
    {
        var item = MakeItem(year: 2024,
            display: new ReleaseDisplay("Der Alte", "Todfeinde"));

        var release = SceneRelease.ForMovie(item, null);

        Assert.Equal("Der.Alte.2024.Todfeinde.GERMAN.1080p.WEB.h264-FunkArr", release.FormatTitle(1080));
    }

    [Fact]
    public void FormatTitle_SpecialCharsRemoved()
    {
        var item = MakeItem(season: "01", episode: "01",
            display: new ReleaseDisplay("Show", "Title: (Special) Edition!"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Equal("Show.S01E01.Title.Special.Edition.GERMAN.720p.WEB.h264-FunkArr", release.FormatTitle(720));
    }

    [Fact]
    public void FormatTitle_UmlautsPreserved()
    {
        var item = MakeItem(display: new ReleaseDisplay("Überführung", "Schöne Grüße"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Contains("Überführung", release.FormatTitle(720));
        Assert.Contains("Schöne.Grüße", release.FormatTitle(720));
    }

    [Fact]
    public void FormatTitle_EszettPreserved()
    {
        var item = MakeItem(display: new ReleaseDisplay("Straße", "Spaß"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Equal("Straße.Spaß.GERMAN.720p.WEB.h264-FunkArr", release.FormatTitle(720));
    }

    [Fact]
    public void FormatTitle_ConsecutiveDotsCollapsed()
    {
        var item = MakeItem(display: new ReleaseDisplay("Show", "A & B"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Contains("Show.A.B", release.FormatTitle(720));
    }

    [Theory]
    [InlineData(1080, "1080p")]
    [InlineData(720, "720p")]
    [InlineData(480, "480p")]
    [InlineData(270, "270p")]
    public void FormatTitle_QualityMapping(int quality, string expected)
    {
        var item = MakeItem(display: new ReleaseDisplay("Show", "Title"));

        var release = SceneRelease.ForShow(item, null);

        Assert.Contains(expected, release.FormatTitle(quality));
    }

    [Fact]
    public void FormatTitle_IdentifierNotDuplicated()
    {
        var item = MakeItem(season: "2026", episode: "10",
            display: new ReleaseDisplay("Leschs Kosmos", "Jahrhunderthitze"));

        var release = SceneRelease.ForShow(item, null);
        var title = release.FormatTitle(1080);

        Assert.Equal("Leschs.Kosmos.S2026E10.Jahrhunderthitze.GERMAN.1080p.WEB.h264-FunkArr", title);
        Assert.Equal(1, CountOccurrences(title, "S2026E10"));
    }

    // --- Expand ---

    [Fact]
    public void Expand_AllThreeUrls_ThreeVariants()
    {
        var item = MakeItem(urlHd: "https://hd.mp4", url: "https://sd.mp4", urlLow: "https://low.mp4",
            display: new ReleaseDisplay("Tatort", "Test"));

        var results = SceneRelease.ForShow(item, null).Expand();

        Assert.Equal(3, results.Length);
        Assert.Contains(results, r => r is { Quality: 1080, Url: "https://hd.mp4" });
        Assert.Contains(results, r => r is { Quality: 720, Url: "https://sd.mp4" });
        Assert.Contains(results, r => r is { Quality: 480, Url: "https://low.mp4" });
    }

    [Fact]
    public void Expand_OnlyNormalUrl_OneVariant()
    {
        var item = MakeItem(url: "https://sd.mp4",
            display: new ReleaseDisplay("Tatort", "Test"));

        var results = SceneRelease.ForShow(item, null).Expand();

        var result = Assert.Single(results);
        Assert.Equal(720, result.Quality);
    }

    [Fact]
    public void Expand_NoUrls_Empty()
    {
        var item = MakeItem(display: new ReleaseDisplay("Tatort", "Test"));

        var results = SceneRelease.ForShow(item, null).Expand();

        Assert.Empty(results);
    }

    [Fact]
    public void Expand_EstimatesSizeWhenZero()
    {
        var item = MakeItem(url: "https://sd.mp4", size: 0, duration: 3600,
            display: new ReleaseDisplay("Tatort", "Test"));

        var results = SceneRelease.ForShow(item, null).Expand();

        var result = Assert.Single(results);
        Assert.Equal(3600L * 420_000L, result.Size);
    }

    [Fact]
    public void Expand_UsesSourceSizeWhenPositive()
    {
        var item = MakeItem(url: "https://sd.mp4", size: 999999,
            display: new ReleaseDisplay("Tatort", "Test"));

        var results = SceneRelease.ForShow(item, null).Expand();

        var result = Assert.Single(results);
        Assert.Equal(999999, result.Size);
    }

    [Fact]
    public void Expand_ShowMetadataPopulated()
    {
        var item = MakeItem(url: "https://sd.mp4", season: "2", episode: "5",
            tvdbId: 83214, imdbId: "tt123",
            display: new ReleaseDisplay("Tatort", "Test"),
            match: new MatchInfo(0.9f, MatchMethod.AirdateMatch));

        var results = SceneRelease.ForShow(item, null).Expand();

        var result = Assert.Single(results);
        Assert.NotNull(result.Metadata);
        Assert.Equal(83214, result.Metadata.Ids?.TvdbId);
        Assert.Equal("tt123", result.Metadata.Ids?.ImdbId);
        Assert.Null(result.Metadata.Ids?.TmdbId);
        Assert.Equal("2", result.Metadata.Season);
        Assert.Equal("5", result.Metadata.Episode);
        Assert.Equal(0.9f, result.Metadata.MatchConfidence);
        Assert.Equal(MatchMethod.AirdateMatch, result.Metadata.MatchMethod);
    }

    [Fact]
    public void Expand_MovieMetadataPopulated()
    {
        var item = MakeItem(url: "https://sd.mp4", year: 1999,
            imdbId: "tt0137523", tmdbId: 550,
            display: new ReleaseDisplay("Fight Club", "Test"),
            match: new MatchInfo(0.85f, MatchMethod.TitleMatch));

        var results = SceneRelease.ForMovie(item, null).Expand();

        var result = Assert.Single(results);
        Assert.NotNull(result.Metadata);
        Assert.Null(result.Metadata.Ids?.TvdbId);
        Assert.Equal("tt0137523", result.Metadata.Ids?.ImdbId);
        Assert.Equal(550, result.Metadata.Ids?.TmdbId);
        Assert.Null(result.Metadata.Season);
        Assert.Null(result.Metadata.Episode);
        Assert.Equal(0.85f, result.Metadata.MatchConfidence);
        Assert.Equal(MatchMethod.TitleMatch, result.Metadata.MatchMethod);
    }

    // --- Helpers ---

    private static EnrichedItem MakeItem(
        string? season = null, string? episode = null, int? year = null,
        int? tvdbId = null, string? imdbId = null, int? tmdbId = null,
        string? urlHd = null, string? url = null, string? urlLow = null,
        long size = 0, int duration = 5400,
        string title = "Test", string topic = "Topic",
        DateTimeOffset? airedAt = null,
        ReleaseDisplay? display = null,
        MatchInfo? match = null)
    {
        MediaIdentity identity = year is not null || tmdbId is not null
            ? new MovieIdentity(imdbId, tmdbId, year)
            : new ShowIdentity(imdbId, tvdbId, season, episode);

        return new EnrichedItem(0,
            new SourceInfo(
                "ARD", topic, title, null, duration, size, airedAt,
                urlHd, url, urlLow, null),
            0.9, true, true,
            identity,
            match,
            display);
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
