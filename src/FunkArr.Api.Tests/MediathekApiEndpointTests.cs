using FunkArr.Messages.Mediathek;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FunkArr.Api.Tests;

public sealed class MediathekApiEndpointTests
{
    [Fact]
    public void MapMediathekApi_registers_search_endpoint()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapMediathekApi();

        var endpoints = app as IEndpointRouteBuilder;
        var dataSource = endpoints.DataSources;

        Assert.NotEmpty(dataSource);
    }

    [Fact]
    public void ToSearchResult_maps_all_fields()
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
            UrlVideo: "https://example.com/med.mp4",
            UrlVideoHd: "https://example.com/hd.mp4",
            UrlSubtitle: "https://example.com/sub.srt",
            UrlWebsite: "https://example.com/page");

        var result = MediathekApiEndpoints.ToSearchResult(item);

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
    public void ToSearchResult_flags_false_when_no_urls()
    {
        var item = new MediathekItem(
            Channel: "ZDF",
            Topic: "Test",
            Title: "Test",
            Description: null,
            Timestamp: 0,
            Duration: 60,
            Size: 1000,
            UrlVideoLow: "https://example.com/low.mp4",
            UrlVideo: null,
            UrlVideoHd: null,
            UrlSubtitle: null,
            UrlWebsite: null);

        var result = MediathekApiEndpoints.ToSearchResult(item);

        Assert.Equal(480, result.Quality);
        Assert.False(result.HasSubtitles);
        Assert.False(result.HasHd);
        Assert.Null(result.WebsiteUrl);
    }

    [Fact]
    public void EstimateQuality_picks_highest_available()
    {
        var hd = new MediathekItem("", "", "", null, 0, 0, 0, null, "x", "x", null, null);
        var med = new MediathekItem("", "", "", null, 0, 0, 0, null, "x", null, null, null);
        var low = new MediathekItem("", "", "", null, 0, 0, 0, "x", null, null, null, null);
        var none = new MediathekItem("", "", "", null, 0, 0, 0, null, null, null, null, null);

        Assert.Equal(1080, MediathekApiEndpoints.EstimateQuality(hd));
        Assert.Equal(720, MediathekApiEndpoints.EstimateQuality(med));
        Assert.Equal(480, MediathekApiEndpoints.EstimateQuality(low));
        Assert.Equal(0, MediathekApiEndpoints.EstimateQuality(none));
    }
}
