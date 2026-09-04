using FunkArr.Core;
using Microsoft.Extensions.Options;

namespace FunkArr.MetadataResolver.Tests;

public sealed class TvdbClientTests
{
    private static TvdbClient CreateClient(string apiKey = "")
    {
        var options = new TvdbOptions { ApiKey = apiKey };
        var monitor = new TestOptionsMonitor<TvdbOptions>(options);
        var factory = new TestHttpClientFactory();
        return new TvdbClient(factory, monitor);
    }

    [Fact]
    public void IsConfigured_returns_false_when_api_key_empty()
    {
        var client = CreateClient("");

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
        var client = CreateClient("");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetEpisodesAsync(83214, null));
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
