using FunkArr.Api.Extensions;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Api.Tests.Extensions;

public sealed class MediathekMappingExtensionsTests
{
    [Fact]
    public void Maps_all_fields_with_hd_url()
    {
        var item = new MediathekItem(
            Channel: "ARD",
            Topic: "Tagesschau",
            Title: "Tagesschau 20:00",
            Description: "Nachrichten",
            Timestamp: 1725300000,
            Duration: 900,
            Size: 245_000_000,
            UrlVideoLow: "https://example.com/low.mp4",
            UrlVideo: "https://example.com/mid.mp4",
            UrlVideoHd: "https://example.com/hd.mp4",
            UrlSubtitle: "https://example.com/sub.xml",
            UrlWebsite: "https://example.com/page");

        var result = item.ToApi();

        Assert.Equal("Tagesschau 20:00", result.Title);
        Assert.Equal("Tagesschau", result.Topic);
        Assert.Equal("ARD", result.Channel);
        Assert.Equal(900, result.Duration);
        Assert.Equal(1080, result.Quality);
        Assert.Equal("Nachrichten", result.Description);
        Assert.Equal(1725300000, result.Timestamp);
        Assert.Equal(245_000_000, result.Size);
        Assert.True(result.HasSubtitles);
        Assert.True(result.HasHd);
        Assert.Equal("https://example.com/page", result.WebsiteUrl);
    }

    [Fact]
    public void Estimates_quality_720_when_only_standard_url()
    {
        var item = new MediathekItem(
            Channel: "ZDF",
            Topic: "heute",
            Title: "heute 19:00",
            Description: null,
            Timestamp: 0,
            Duration: 600,
            Size: 100_000_000,
            UrlVideoLow: "https://example.com/low.mp4",
            UrlVideo: "https://example.com/mid.mp4",
            UrlVideoHd: null,
            UrlSubtitle: null,
            UrlWebsite: null);

        var result = item.ToApi();

        Assert.Equal(720, result.Quality);
        Assert.False(result.HasHd);
    }

    [Fact]
    public void Estimates_quality_480_when_only_low_url()
    {
        var item = new MediathekItem(
            Channel: "ORF",
            Topic: "ZIB",
            Title: "ZIB 1",
            Description: null,
            Timestamp: 0,
            Duration: 300,
            Size: 50_000_000,
            UrlVideoLow: "https://example.com/low.mp4",
            UrlVideo: null,
            UrlVideoHd: null,
            UrlSubtitle: null,
            UrlWebsite: null);

        var result = item.ToApi();

        Assert.Equal(480, result.Quality);
        Assert.False(result.HasHd);
    }

    [Fact]
    public void Estimates_quality_0_when_no_video_urls()
    {
        var item = new MediathekItem(
            Channel: "SRF",
            Topic: "Tagesschau",
            Title: "Tagesschau",
            Description: null,
            Timestamp: 0,
            Duration: 0,
            Size: 0,
            UrlVideoLow: null,
            UrlVideo: null,
            UrlVideoHd: null,
            UrlSubtitle: null,
            UrlWebsite: null);

        var result = item.ToApi();

        Assert.Equal(0, result.Quality);
        Assert.False(result.HasHd);
    }

    [Fact]
    public void Has_subtitles_true_when_subtitle_url_set()
    {
        var item = new MediathekItem(
            Channel: "ARD",
            Topic: "Test",
            Title: "Test",
            Description: null,
            Timestamp: 0,
            Duration: 0,
            Size: 0,
            UrlVideoLow: null,
            UrlVideo: null,
            UrlVideoHd: null,
            UrlSubtitle: "https://example.com/sub.xml",
            UrlWebsite: null);

        var result = item.ToApi();

        Assert.True(result.HasSubtitles);
    }

    [Fact]
    public void Has_subtitles_false_when_subtitle_url_null()
    {
        var item = new MediathekItem(
            Channel: "ARD",
            Topic: "Test",
            Title: "Test",
            Description: null,
            Timestamp: 0,
            Duration: 0,
            Size: 0,
            UrlVideoLow: null,
            UrlVideo: null,
            UrlVideoHd: null,
            UrlSubtitle: null,
            UrlWebsite: null);

        var result = item.ToApi();

        Assert.False(result.HasSubtitles);
    }

    [Fact]
    public void Passes_through_nullable_fields()
    {
        var withValues = new MediathekItem(
            Channel: "ARD",
            Topic: "Test",
            Title: "Test",
            Description: "A description",
            Timestamp: 0,
            Duration: 0,
            Size: 0,
            UrlVideoLow: null,
            UrlVideo: null,
            UrlVideoHd: null,
            UrlSubtitle: null,
            UrlWebsite: "https://example.com");

        var withNulls = new MediathekItem(
            Channel: "ARD",
            Topic: "Test",
            Title: "Test",
            Description: null,
            Timestamp: 0,
            Duration: 0,
            Size: 0,
            UrlVideoLow: null,
            UrlVideo: null,
            UrlVideoHd: null,
            UrlSubtitle: null,
            UrlWebsite: null);

        var resultWithValues = withValues.ToApi();
        var resultWithNulls = withNulls.ToApi();

        Assert.Equal("A description", resultWithValues.Description);
        Assert.Equal("https://example.com", resultWithValues.WebsiteUrl);
        Assert.Null(resultWithNulls.Description);
        Assert.Null(resultWithNulls.WebsiteUrl);
    }
}
