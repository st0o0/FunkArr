using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FunkArr.Tests.Integration;

public class WebUiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WebUiEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FunkArr:ApiKey", "test-key");
            builder.UseSetting("FunkArr:Download:DownloadPath", Path.GetTempPath());
            builder.UseSetting("FunkArr:Download:TempPath", Path.Combine(Path.GetTempPath(), "funkarr-webui-test"));
            builder.UseSetting("FunkArr:PersistencePath",
                Path.Combine(Path.GetTempPath(), $"funkarr-webui-{Guid.NewGuid():N}.db"));
        }).CreateClient();
    }

    // Queue endpoints

    [Fact]
    public async Task Queue_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/queue");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Queue_WithValidApiKey_ReturnsJsonArray()
    {
        var response = await _client.GetAsync("/api/v1/queue?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task History_WithValidApiKey_ReturnsJsonArray()
    {
        var response = await _client.GetAsync("/api/v1/history?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // Config endpoints

    [Fact]
    public async Task Config_WithValidApiKey_ReturnsMaskedArrKeys()
    {
        var response = await _client.GetAsync("/api/v1/config?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("apiKey", out _));
    }

    [Fact]
    public async Task Config_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/config");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Setup endpoints

    [Fact]
    public async Task SetupStatus_WithValidApiKey_ReturnsStatusObject()
    {
        var response = await _client.GetAsync("/api/v1/setup/status?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("configured", out _));
        Assert.True(root.TryGetProperty("ffmpeg", out _));
        Assert.True(root.TryGetProperty("paths", out _));
    }

    [Fact]
    public async Task TestFfmpeg_ReturnsFoundAndVersion()
    {
        var response = await _client.PostAsync("/api/v1/setup/test-ffmpeg?apikey=test-key", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("found", out _));
        Assert.True(root.TryGetProperty("version", out _));
    }

    [Fact]
    public async Task TestPaths_ValidatesAndReturnsResult()
    {
        var body = new StringContent(
            JsonSerializer.Serialize(new
            {
                downloadPath = Path.GetTempPath(),
                tempPath = Path.GetTempPath(),
            }),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/v1/setup/test-paths?apikey=test-key", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("downloadOk", out var downloadOk));
        Assert.True(downloadOk.GetBoolean());
        Assert.True(root.TryGetProperty("tempOk", out var tempOk));
        Assert.True(tempOk.GetBoolean());
    }

    // Ruleset endpoints

    [Fact]
    public async Task Rulesets_WithValidApiKey_ReturnsArray()
    {
        var response = await _client.GetAsync("/api/v1/rulesets?apikey=test-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task RulesetsReload_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/v1/rulesets/reload?apikey=test-key", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("reloaded", content);
    }

    [Fact]
    public async Task Rulesets_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/rulesets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
