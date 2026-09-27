using System.Net;
using System.Net.Http.Json;
using FunkArr.Api;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;

namespace FunkArr.IntegrationTests.Api;

[Collection("System")]
public sealed class SystemTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Version_ReturnsVersionResponse()
    {
        var response = await _fixture.Client.GetAsync("/api/system/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<VersionResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.AppVersion);
    }

    [Fact]
    public async Task Storage_ReturnsStorageStatusResponse()
    {
        var response = await _fixture.Client.GetAsync("/api/system/storage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<StorageStatusResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.CompleteDirectory);
        Assert.NotNull(result.IncompleteDirectory);
        Assert.NotNull(result.CompleteDirectory.Path);
        Assert.NotNull(result.IncompleteDirectory.Path);
    }

    [Fact]
    public async Task Cache_ReturnsCacheStats()
    {
        var task = _fixture.Client.GetAsync("/api/system/cache");

        var probe = _fixture.GetProbe<IEnrichmentManager>();
        probe.ExpectMsg<QueryCacheStats>();
        probe.Reply(new CacheStatsResult(42, 17, DateTimeOffset.UtcNow));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CacheStatsResponse>();
        Assert.NotNull(result);
        Assert.Equal(42, result.TvdbEntries);
        Assert.Equal(17, result.TmdbEntries);
        Assert.NotNull(result.OldestEntry);
    }

    [Fact]
    public async Task Logs_ReturnsArray()
    {
        var response = await _fixture.Client.GetAsync("/api/system/logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LogEntry[]>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Routes_ReturnsRoutesResponse()
    {
        var response = await _fixture.Client.GetAsync("/api/system/routes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RoutesResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Definitions);
        Assert.NotNull(result.ChannelRoutes);
        Assert.Equal("Direct", result.DefaultRoute);
    }
}
