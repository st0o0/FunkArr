using FunkArr.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Enrichment.Tests;

public sealed class TmdbClientTests
{
    private static TmdbClient CreateClient(string apiKey = "")
    {
        var options = new TmdbOptions { ApiKey = apiKey };
        var monitor = new TestOptionsMonitor<TmdbOptions>(options);
        var cache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        return new TmdbClient(new HttpClient(), monitor, cache, NullLogger<TmdbClient>.Instance);
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
    public async Task GetMovieDataAsync_throws_when_not_configured()
    {
        var client = CreateClient();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMovieDataAsync(550));
    }

    [Fact]
    public async Task FindByImdbIdAsync_throws_when_not_configured()
    {
        var client = CreateClient();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.FindByImdbIdAsync("tt0137523"));
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

}
