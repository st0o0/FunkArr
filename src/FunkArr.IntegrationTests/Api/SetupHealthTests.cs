using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;

namespace FunkArr.IntegrationTests.Api;

[Collection("Setup")]
public sealed class SetupHealthTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task SetupHealth_ReturnsCheckEntries()
    {
        var response = await _fixture.Client.GetAsync("/api/system/setup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SetupHealthCheck>();
        Assert.NotNull(result);
        Assert.NotNull(result.Checks);
        Assert.NotNull(result.SetupConnectionInfo);

        // Verify all expected check keys are present
        Assert.True(result.Checks.ContainsKey("apiKey"));
        Assert.True(result.Checks.ContainsKey("mediathekViewWeb"));
        Assert.True(result.Checks.ContainsKey("dataDirectory"));
        Assert.True(result.Checks.ContainsKey("completeDirectory"));
        Assert.True(result.Checks.ContainsKey("incompleteDirectory"));
        Assert.True(result.Checks.ContainsKey("indexerApi"));
        Assert.True(result.Checks.ContainsKey("downloadApi"));
        Assert.True(result.Checks.ContainsKey("ffmpeg"));

        // Verify connection info structure
        Assert.Equal("/index/api", result.SetupConnectionInfo.IndexerApiPath);
        Assert.Equal("/download/api", result.SetupConnectionInfo.DownloadApiPath);
    }
}

// Setup provision tests (prowlarr/sonarr/radarr indexer + download client creation)
// are skipped because they call external Arr APIs which cannot be easily mocked
// in this fixture without a fake HTTP server for ArrSetupClient.
