using System.Net;
using System.Text;
using System.Text.Json;
using FunkArr.Setup;
using FunkArr.Tests.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.Tests.Integration;

public class SetupValidationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SetupValidationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FunkArr:ApiKey", "test-key");
            builder.UseSetting("FunkArr:Download:DownloadPath", Path.GetTempPath());
            builder.UseSetting("FunkArr:Download:TempPath", Path.Combine(Path.GetTempPath(), "funkarr-setup-validate-test"));
            builder.UseSetting("FunkArr:PersistencePath",
                Path.Combine(Path.GetTempPath(), $"funkarr-setup-validate-{Guid.NewGuid():N}.db"));
        }).CreateClient();
    }

    [Fact]
    public async Task Validate_WithoutApiKey_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/setup/validate", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithValidApiKeyAndEmptyBody_ReturnsOnlySelfChecks()
    {
        var response = await _client.PostAsync("/api/v1/setup/validate?apikey=test-key", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("overallStatus", out _));
        Assert.True(root.TryGetProperty("checks", out var checks));
        Assert.Equal(JsonValueKind.Array, checks.ValueKind);

        var names = checks.EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("api-key", names);
        Assert.Contains("ffmpeg", names);
        Assert.Contains("download-path", names);
        Assert.Contains("temp-path", names);
    }

    [Fact]
    public async Task Validate_WithUnreachableProwlarr_ReturnsOkNotServerError()
    {
        var body = new StringContent(
            JsonSerializer.Serialize(new
            {
                prowlarr = new { url = "http://127.0.0.1:1", apiKey = "does-not-matter" },
            }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/v1/setup/validate?apikey=test-key", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var names = doc.RootElement.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("prowlarr-connectivity", names);
        Assert.Contains("prowlarr-registered", names);
    }

    [Fact]
    public async Task Validate_WithFullRequestBody_ReturnsSelfAndExternalChecks()
    {
        var indexerJson = """
        [ { "name": "FunkArr", "fields": [ { "name": "baseUrl", "value": "http://funkarr:9797" } ] } ]
        """;
        var downloadClientJson = """
        [ { "name": "FunkArr", "fields": [ { "name": "host", "value": "funkarr" } ] } ]
        """;

        var handler = new FakeHttpMessageHandler(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("health") || path.Contains("system/status"))
                return new HttpResponseMessage(HttpStatusCode.OK);
            if (path.Contains("indexer"))
                return FakeHttpMessageHandler.JsonResponse(indexerJson);

            return FakeHttpMessageHandler.JsonResponse(downloadClientJson);
        });

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FunkArr:ApiKey", "test-key");
            builder.UseSetting("FunkArr:Download:DownloadPath", Path.GetTempPath());
            builder.UseSetting("FunkArr:Download:TempPath", Path.Combine(Path.GetTempPath(), "funkarr-setup-validate-full"));
            builder.UseSetting("FunkArr:PersistencePath",
                Path.Combine(Path.GetTempPath(), $"funkarr-setup-validate-full-{Guid.NewGuid():N}.db"));

            builder.ConfigureServices(services =>
            {
                services.AddHttpClient(nameof(SetupValidationService))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
            });
        });
        var client = factory.CreateClient();

        var body = new StringContent(
            JsonSerializer.Serialize(new
            {
                prowlarr = new { url = "http://prowlarr:9696", apiKey = "key" },
                arrInstances = new[]
                {
                    new { name = "Sonarr", type = "Sonarr", url = "http://sonarr:8989", apiKey = "key" },
                },
                selfUrl = "http://funkarr:9797",
            }),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/setup/validate?apikey=test-key", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var checks = doc.RootElement.GetProperty("checks");
        var byName = checks.EnumerateArray()
            .ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("status").GetString());

        Assert.Equal("pass", byName["api-key"]);
        Assert.Equal("pass", byName["prowlarr-connectivity"]);
        Assert.Equal("pass", byName["prowlarr-registered"]);
        Assert.Equal("pass", byName["Sonarr-connectivity"]);
        Assert.Equal("pass", byName["Sonarr-registered"]);
    }
}
