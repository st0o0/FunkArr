using System.Net;
using System.Text.Json;
using FunkArr.Core.Configuration;
using FunkArr.Enrichment.Tvdb;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Enrichment.Tests.Tvdb;

public sealed class TvdbClientCacheTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static TvdbClient CreateClient(HttpMessageHandler handler, MemoryCache? cache = null)
    {
        var options = new TvdbOptions { ApiKey = "test-api-key" };
        var monitor = new TestOptionsMonitor<TvdbOptions>(options);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api4.thetvdb.com/v4/") };
        return new TvdbClient(httpClient, monitor, cache ?? new MemoryCache(new MemoryCacheOptions()), NullLogger<TvdbClient>.Instance);
    }

    [Fact]
    public async Task Full_success_is_cached()
    {
        var callCount = 0;
        var handler = new MockHandler(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                return LoginResponse();
            }

            callCount++;
            return EpisodesResponse([new TvdbEpisode(1, 1, "Pilot", "2026-01-01", 60)]);
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var first = await client.GetEpisodesAsync(83214);
        var second = await client.GetEpisodesAsync(83214);

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Partial_failure_is_not_cached()
    {
        var callCount = 0;
        var handler = new MockHandler(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                return LoginResponse();
            }

            callCount++;
            var uri = request.RequestUri.ToString();
            if (uri.Contains("page=0"))
            {
                return EpisodesResponse(
                    [new TvdbEpisode(1, 1, "Ep1", "2026-01-01", 60)],
                    hasNextPage: true);
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var first = await client.GetEpisodesAsync(83214);
        Assert.Single(first);

        var second = await client.GetEpisodesAsync(83214);
        Assert.Equal(4, callCount);
    }

    [Fact]
    public async Task Pagination_combines_two_pages()
    {
        var handler = new MockHandler(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                return LoginResponse();
            }

            var uri = request.RequestUri.ToString();
            if (uri.Contains("page=0"))
            {
                return EpisodesResponse(
                    [new TvdbEpisode(1, 1, "Ep1", "2026-01-01", 60)],
                    hasNextPage: true);
            }

            return EpisodesResponse(
                [new TvdbEpisode(1, 2, "Ep2", "2026-01-08", 60)]);
        });

        var client = CreateClient(handler);

        var episodes = await client.GetEpisodesAsync(83214);

        Assert.Equal(2, episodes.Length);
        Assert.Equal("Ep1", episodes[0].Name);
        Assert.Equal("Ep2", episodes[1].Name);
    }

    [Fact]
    public async Task Reauth_on_401_succeeds()
    {
        var authAttempts = 0;
        var episodeAttempts = 0;
        var handler = new MockHandler(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                authAttempts++;
                return LoginResponse();
            }

            episodeAttempts++;
            if (episodeAttempts == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return EpisodesResponse([new TvdbEpisode(1, 1, "Pilot", "2026-01-01", 60)]);
        });

        var client = CreateClient(handler);

        var episodes = await client.GetEpisodesAsync(83214);

        Assert.Single(episodes);
        Assert.Equal(2, authAttempts);
    }

    [Fact]
    public async Task Reauth_on_401_fails_returns_empty_not_cached()
    {
        var handler = new MockHandler(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                return LoginResponse();
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var first = await client.GetEpisodesAsync(83214);
        Assert.Empty(first);

        var second = await client.GetEpisodesAsync(83214);
        Assert.Empty(second);
    }

    [Fact]
    public async Task Cache_hit_skips_http_call()
    {
        var httpCalls = 0;
        var handler = new MockHandler(request =>
        {
            httpCalls++;
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                return LoginResponse();
            }

            return EpisodesResponse([new TvdbEpisode(1, 1, "Pilot", "2026-01-01", 60)]);
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        await client.GetEpisodesAsync(83214);
        var callsAfterFirst = httpCalls;

        await client.GetEpisodesAsync(83214);
        Assert.Equal(callsAfterFirst, httpCalls);
    }

    [Fact]
    public async Task First_page_failure_returns_empty_and_is_not_cached()
    {
        var callCount = 0;
        var handler = new MockHandler(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                return LoginResponse();
            }

            callCount++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var first = await client.GetEpisodesAsync(83214);
        Assert.Empty(first);

        var second = await client.GetEpisodesAsync(83214);
        Assert.Empty(second);
        Assert.Equal(2, callCount);
    }

    private static HttpResponseMessage LoginResponse()
    {
        var json = JsonSerializer.Serialize(new { data = new { token = "test-token" } }, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage EpisodesResponse(TvdbEpisode[] episodes, bool hasNextPage = false)
    {
        var body = new
        {
            data = new { episodes },
            links = new { next = hasNextPage ? "next" : (string?)null }
        };
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class MockHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
