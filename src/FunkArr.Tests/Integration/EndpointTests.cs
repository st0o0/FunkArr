using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FunkArr.Tests.Integration;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FunkArr:ApiKey", "test-key");
            builder.UseSetting("FunkArr:Download:DownloadPath", Path.GetTempPath());
            builder.UseSetting("FunkArr:Download:TempPath", Path.Combine(Path.GetTempPath(), "funkarr-int-test"));
            builder.UseSetting("FunkArr:PersistencePath",
                Path.Combine(Path.GetTempPath(), $"funkarr-int-{Guid.NewGuid():N}.db"));
        }).CreateClient();
    }

    [Fact]
    public async Task Alive_ReturnsOk()
    {
        var response = await _client.GetAsync("/alive");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Healthz_ReturnsOk()
    {
        var response = await _client.GetAsync("/healthz");
        var status = (int)response.StatusCode;
        Assert.True(status is 200 or 503);
    }

    [Fact]
    public async Task Newznab_Caps_WithValidKey_ReturnsXml()
    {
        var response = await _client.GetAsync("/api?t=caps&apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<caps>", content);
        Assert.Contains("FunkArr", content);
    }

    [Fact]
    public async Task Newznab_Caps_WithInvalidKey_ReturnsError()
    {
        var response = await _client.GetAsync("/api?t=caps&apikey=wrong-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Incorrect user credentials", content);
    }

    [Fact]
    public async Task Newznab_TvSearch_ReturnsXml()
    {
        var response = await _client.GetAsync("/api?t=tvsearch&tvdbid=12345&apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<rss", content);
    }

    [Fact]
    public async Task Sabnzbd_Version_ReturnsVersion()
    {
        var response = await _client.GetAsync("/download/api?mode=version&apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("4.3.3", content);
    }

    [Fact]
    public async Task Sabnzbd_Queue_ReturnsEmptyQueue()
    {
        var response = await _client.GetAsync("/download/api?mode=queue&apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("slots", content);
    }

    [Fact]
    public async Task Sabnzbd_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/download/api?mode=version");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MatchIntelligence_RecentMatches_WithValidKey_ReturnsJson()
    {
        var response = await _client.GetAsync("/api/v1/matches/recent?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content);
    }

    [Fact]
    public async Task MatchIntelligence_RecentMatches_WithoutKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/matches/recent");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MatchIntelligence_Topics_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/v1/matches/topics?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content);
    }

    [Fact]
    public async Task MatchIntelligence_TopicDetail_UnknownTopic_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/matches/topics/nonexistent?apikey=test-key");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MatchIntelligence_Unmatched_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/v1/matches/unmatched?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content);
    }
}
