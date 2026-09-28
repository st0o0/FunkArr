using FunkArr.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Enrichment.Tests;

public sealed class TvdbClientTests
{
    private static TvdbClient CreateClient(string apiKey = "")
    {
        var options = new TvdbOptions { ApiKey = apiKey };
        var monitor = new TestOptionsMonitor<TvdbOptions>(options);
        var cache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        return new TvdbClient(new HttpClient(), monitor, cache, NullLogger<TvdbClient>.Instance, TimeProvider.System);
    }

    [Fact]
    public void IsConfigured_returns_false_when_api_key_empty()
    {
        var client = CreateClient();

        Assert.False(client.IsConfigured);
    }

    [Fact]
    public void IsConfigured_returns_true_when_api_key_set()
    {
        var client = CreateClient("test-api-key");

        Assert.True(client.IsConfigured);
    }

    [Fact]
    public async Task GetEpisodesAsync_throws_when_not_configured()
    {
        var client = CreateClient();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetEpisodesAsync(83214));
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

}
