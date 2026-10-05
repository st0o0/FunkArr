using System.Net;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Messages.Mediathek;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.Api.Tests;

public sealed class MediathekApiEndpointTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private ActorSystem _actorSystem = null!;

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        _actorSystem = ActorSystem.Create("mediathekapi-endpoint-tests");
        var registry = ActorRegistry.For(_actorSystem);
        builder.Services.AddSingleton<IActorRegistry>(registry);

        _app = builder.Build();
        _app.MapMediathekApi();

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _actorSystem.Terminate();
    }

    [Fact]
    public async Task Search_without_query_terms_returns_bad_request()
    {
        var response = await Client.GetAsync("/api/mediathek/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("At least one of q, channel, or topic is required", body);
    }

    [Fact]
    public void ToApi_maps_all_fields()
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
    public void ToApi_flags_false_when_no_urls()
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

        var result = item.ToApi();

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

        Assert.Equal(1080, MediathekMappingExtensions.EstimateQuality(hd));
        Assert.Equal(720, MediathekMappingExtensions.EstimateQuality(med));
        Assert.Equal(480, MediathekMappingExtensions.EstimateQuality(low));
        Assert.Equal(0, MediathekMappingExtensions.EstimateQuality(none));
    }
}
