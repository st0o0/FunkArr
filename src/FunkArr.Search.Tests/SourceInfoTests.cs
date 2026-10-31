using FunkArr.Messages.Mediathek;

namespace FunkArr.Search.Tests;

public sealed class SourceInfoTests
{
    [Fact]
    public void From_converts_positive_timestamp_to_DateTimeOffset()
    {
        var item = MakeItem(timestamp: 1719244800);

        var source = SourceInfo.From(item);

        Assert.NotNull(source.AiredAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1719244800), source.AiredAt);
    }

    [Fact]
    public void From_zero_timestamp_produces_null_AiredAt()
    {
        var item = MakeItem(timestamp: 0);

        var source = SourceInfo.From(item);

        Assert.Null(source.AiredAt);
    }

    [Fact]
    public void From_maps_URL_fields_correctly()
    {
        var item = new MediathekItem("ARD", "Tatort", "Test", null, 0, 5400, 100,
            "https://low.mp4", "https://normal.mp4", "https://hd.mp4", "https://sub.vtt", null);

        var source = SourceInfo.From(item);

        Assert.Equal("https://hd.mp4", source.UrlHd);
        Assert.Equal("https://normal.mp4", source.Url);
        Assert.Equal("https://low.mp4", source.UrlLow);
        Assert.Equal("https://sub.vtt", source.SubtitleUrl);
    }

    [Fact]
    public void From_maps_null_URLs()
    {
        var item = MakeItem();

        var source = SourceInfo.From(item);

        Assert.Null(source.UrlHd);
        Assert.Null(source.UrlLow);
        Assert.Null(source.SubtitleUrl);
    }

    [Fact]
    public void From_maps_basic_fields()
    {
        var item = new MediathekItem("ZDF", "Show", "Episode 1", "desc", 1719244800, 3600, 500,
            null, "https://v.mp4", null, null, null);

        var source = SourceInfo.From(item);

        Assert.Equal("ZDF", source.Channel);
        Assert.Equal("Show", source.Topic);
        Assert.Equal("Episode 1", source.Title);
        Assert.Equal("desc", source.Description);
        Assert.Equal(3600, source.Duration);
        Assert.Equal(500, source.Size);
    }

    [Fact]
    public void ResolveQuality_returns_1080_when_UrlHd_set()
    {
        var source = MakeSource(urlHd: "https://hd.mp4", url: "https://sd.mp4");

        Assert.Equal(1080, source.ResolveQuality());
    }

    [Fact]
    public void ResolveQuality_returns_720_when_only_Url_set()
    {
        var source = MakeSource(url: "https://sd.mp4");

        Assert.Equal(720, source.ResolveQuality());
    }

    [Fact]
    public void ResolveQuality_returns_0_when_no_URLs()
    {
        var source = MakeSource();

        Assert.Equal(0, source.ResolveQuality());
    }

    private static MediathekItem MakeItem(long timestamp = 0) =>
        new("ARD", "Tatort", "Test", null, timestamp, 5400, 0,
            null, "https://v.mp4", null, null, null);

    private static SourceInfo MakeSource(
        string? urlHd = null, string? url = null, string? urlLow = null) =>
        new("ARD", "Tatort", "Test", null, 5400, 0, null, urlHd, url, urlLow, null);
}
