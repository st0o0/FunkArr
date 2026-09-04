using FunkArr.Core;
using Microsoft.Extensions.Options;

namespace FunkArr.MetadataResolver.Tests;

public sealed class TmdbClientTests
{
    private static TmdbClient CreateClient(string apiKey = "")
    {
        var options = new TmdbOptions { ApiKey = apiKey };
        var monitor = new TestOptionsMonitor<TmdbOptions>(options);
        var factory = new TestHttpClientFactory();
        return new TmdbClient(factory, monitor);
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
    public async Task GetMovieAsync_throws_when_not_configured()
    {
        var client = CreateClient("");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMovieAsync(550));
    }

    [Fact]
    public async Task FindByImdbIdAsync_throws_when_not_configured()
    {
        var client = CreateClient("");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.FindByImdbIdAsync("tt0137523"));
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
