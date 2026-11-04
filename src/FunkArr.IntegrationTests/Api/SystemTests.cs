using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Infrastructure;
using FunkArr.Api.Models.Shared;
using FunkArr.Api.Models.System;
using FunkArr.Core.Actors;
using FunkArr.Messages.Enrichment;
using VariantPolicy = FunkArr.Api.Models.Shared.VariantPolicy;

namespace FunkArr.IntegrationTests.Api;

[Collection("System")]
public sealed class SystemTests(FunkArrFixture fixture)
{
    [Fact]
    public async Task Version_ReturnsVersionResponse()
    {
        var response = await fixture.Client.GetAsync("/api/system/version", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<VersionResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.AppVersion);
    }

    [Fact]
    public async Task Storage_ReturnsStorageStatusResponse()
    {
        var response = await fixture.Client.GetAsync("/api/system/storage", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<StorageStatusResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.CompleteDirectory);
        Assert.NotNull(result.IncompleteDirectory);
        Assert.NotNull(result.CompleteDirectory.Path);
        Assert.NotNull(result.IncompleteDirectory.Path);
    }

    [Fact]
    public async Task Cache_ReturnsCacheStats()
    {
        var task = fixture.Client.GetAsync("/api/system/cache", TestContext.Current.CancellationToken);

        var probe = fixture.GetProbe<IEnrichmentManager>();
        probe.ExpectMsg<QueryCacheStats>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new CacheStatsResult(42, 17, DateTimeOffset.UtcNow));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CacheStatsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(42, result.TvdbEntries);
        Assert.Equal(17, result.TmdbEntries);
        Assert.NotNull(result.OldestEntry);
    }

    [Fact]
    public async Task Logs_ReturnsArray()
    {
        var response = await fixture.Client.GetAsync("/api/system/logs", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LogEntry[]>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Routes_ReturnsRoutesResponse()
    {
        var response = await fixture.Client.GetAsync("/api/system/routes", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RoutesResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Definitions);
        Assert.NotNull(result.ChannelRoutes);
        Assert.Equal("Direct", result.DefaultRoute);
    }

    [Fact]
    public async Task Variants_ReturnsDefaultPolicy()
    {
        var response = await fixture.Client.GetAsync("/api/system/variants", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<VariantsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        var expected = new[]
        {
            MediathekVariant.AudioDescription,
            MediathekVariant.SignLanguage,
            MediathekVariant.PlainLanguage,
            MediathekVariant.OriginalVersion
        };
        Assert.Equal(expected, [.. result.Variants.Select(v => v.Variant)]);
        Assert.All(result.Variants, v => Assert.Equal(VariantPolicy.Exclude, v.Policy));
    }
}
