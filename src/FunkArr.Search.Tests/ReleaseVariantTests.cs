using FunkArr.Messages;
using FunkArr.Messages.Enrichment;

namespace FunkArr.Search.Tests;

public sealed class ReleaseVariantTests
{
    [Fact]
    public void Expand_with_all_three_URLs_produces_three_variants()
    {
        var item = MakeItem(urlHd: "https://hd.mp4", url: "https://sd.mp4", urlLow: "https://low.mp4");

        var variants = ReleaseVariant.Expand(item, MediaType.Show, "Tatort");

        Assert.Equal(3, variants.Length);
        Assert.Contains(variants, v => v.Quality == 1080 && v.Url == "https://hd.mp4");
        Assert.Contains(variants, v => v.Quality == 720 && v.Url == "https://sd.mp4");
        Assert.Contains(variants, v => v.Quality == 480 && v.Url == "https://low.mp4");
    }

    [Fact]
    public void Expand_with_only_normal_URL_produces_one_variant()
    {
        var item = MakeItem(url: "https://sd.mp4");

        var variants = ReleaseVariant.Expand(item, MediaType.Show, null);

        var variant = Assert.Single(variants);
        Assert.Equal(720, variant.Quality);
        Assert.Equal("https://sd.mp4", variant.Url);
    }

    [Fact]
    public void Expand_with_no_URLs_produces_empty_array()
    {
        var item = MakeItem();

        var variants = ReleaseVariant.Expand(item, MediaType.Show, null);

        Assert.Empty(variants);
    }

    [Fact]
    public void Expand_estimates_size_when_source_size_is_zero()
    {
        var item = MakeItem(url: "https://sd.mp4", size: 0, duration: 3600);

        var variants = ReleaseVariant.Expand(item, MediaType.Show, null);

        var variant = Assert.Single(variants);
        Assert.Equal(3600L * 420_000L, variant.Size);
    }

    [Fact]
    public void Expand_uses_source_size_when_positive()
    {
        var item = MakeItem(url: "https://sd.mp4", size: 999999);

        var variants = ReleaseVariant.Expand(item, MediaType.Show, null);

        var variant = Assert.Single(variants);
        Assert.Equal(999999, variant.Size);
    }

    [Fact]
    public void Expand_passes_match_info_through()
    {
        var match = new MatchInfo(0.95f, MatchMethod.TitleMatch);
        var item = MakeItem(url: "https://sd.mp4", match: match);

        var variants = ReleaseVariant.Expand(item, MediaType.Show, null);

        var variant = Assert.Single(variants);
        Assert.NotNull(variant.Match);
        Assert.Equal(0.95f, variant.Match.Confidence);
        Assert.Equal(MatchMethod.TitleMatch, variant.Match.Method);
    }

    [Fact]
    public void ToResultItem_maps_all_fields()
    {
        var source = new SourceInfo("ARD", "Tatort", "Roomservice", "desc", 5400, 1000, DateTimeOffset.UnixEpoch,
            null, "https://sd.mp4", null, "https://sub.vtt");
        var identity = new MediaIdentity(83214, "tt123", 550, "2", "5");
        var match = new MatchInfo(0.9f, MatchMethod.AirdateMatch);
        var variant = new ReleaseVariant("Tatort.S02E05.GERMAN.720p.WEB.h264-FunkArr",
            "https://sd.mp4", source, identity, 0.85, 720, 1000, match);

        var item = variant.ToResultItem();

        Assert.Equal("Tatort.S02E05.GERMAN.720p.WEB.h264-FunkArr", item.Title);
        Assert.Equal("https://sd.mp4", item.Url);
        Assert.Equal("ARD", item.Channel);
        Assert.Equal("Tatort", item.Topic);
        Assert.Equal(5400, item.Duration);
        Assert.Equal(1000, item.Size);
        Assert.Equal(720, item.Quality);
        Assert.Equal(DateTimeOffset.UnixEpoch, item.AiredAt);
        Assert.Equal(0.85, item.Score);
        Assert.Equal("https://sub.vtt", item.SubtitleUrl);
        Assert.Equal(83214, item.TvdbId);
        Assert.Equal("tt123", item.ImdbId);
        Assert.Equal(550, item.TmdbId);
        Assert.Equal("2", item.Season);
        Assert.Equal("5", item.Episode);
        Assert.Equal(0.9f, item.MatchConfidence);
        Assert.Equal(MatchMethod.AirdateMatch, item.MatchMethod);
    }

    [Fact]
    public void ToResultItem_maps_null_match()
    {
        var source = new SourceInfo("ARD", "Tatort", "Test", null, 5400, 0, null,
            null, "https://v.mp4", null, null);
        var identity = new MediaIdentity(null, null, null, null, null);
        var variant = new ReleaseVariant("Title", "https://v.mp4", source, identity, 0.0, 720, 0, null);

        var item = variant.ToResultItem();

        Assert.Null(item.MatchConfidence);
        Assert.Null(item.MatchMethod);
    }

    private static EnrichedItem MakeItem(
        string? urlHd = null, string? url = null, string? urlLow = null,
        long size = 0, int duration = 5400, MatchInfo? match = null) =>
        new(0,
            new SourceInfo("ARD", "Tatort", "Test", null, duration, size, null, urlHd, url, urlLow, null),
            0.9, true, true,
            new MediaIdentity(null, null, null, null, null),
            match);
}
