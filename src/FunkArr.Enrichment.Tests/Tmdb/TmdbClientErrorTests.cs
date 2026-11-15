using System.Net;
using System.Text.Json;
using FunkArr.Core.Configuration;
using FunkArr.Enrichment.Tmdb;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.Enrichment.Tests.Tmdb;

public sealed class TmdbClientErrorTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static TmdbClient CreateClient(HttpMessageHandler handler, ILogger<TmdbClient>? logger = null)
    {
        var options = new TmdbOptions { ApiKey = "secret-api-key-12345" };
        var monitor = new TestOptionsMonitor<TmdbOptions>(options);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.themoviedb.org/3/") };
        return new TmdbClient(httpClient, monitor, cache, logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TmdbClient>.Instance);
    }

    [Fact]
    public async Task NotFound_404_returns_null()
    {
        var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(handler);

        var result = await client.GetMovieDataAsync(999999);

        Assert.Null(result);
    }

    [Fact]
    public async Task TooManyRequests_429_throws_HttpRequestException()
    {
        var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMovieDataAsync(550));
    }

    [Fact]
    public async Task ServerError_503_throws_HttpRequestException()
    {
        var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMovieDataAsync(550));
    }

    [Fact]
    public async Task Success_200_returns_movie_data()
    {
        var handler = new MockHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("alternative_titles"))
            {
                return JsonResponse(new { titles = Array.Empty<object>() });
            }

            return JsonResponse(new
            {
                id = 550,
                title = "Fight Club",
                original_title = "Fight Club",
                release_date = "1999-10-15",
                runtime = 139,
                imdb_id = "tt0137523"
            });
        });
        var client = CreateClient(handler);

        var result = await client.GetMovieDataAsync(550);

        Assert.NotNull(result);
        Assert.Equal("Fight Club", result.Movie.Title);
    }

    [Fact]
    public async Task Api_key_not_logged_on_failure()
    {
        var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var logSink = new CapturingLogger();
        var client = CreateClient(handler, logSink);

        await client.GetMovieDataAsync(999999);

        foreach (var message in logSink.Messages)
        {
            Assert.DoesNotContain("secret-api-key-12345", message);
        }
    }

    [Fact]
    public async Task Api_key_not_logged_on_transient_error()
    {
        var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var logSink = new CapturingLogger();
        var client = CreateClient(handler, logSink);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMovieDataAsync(550));

        foreach (var message in logSink.Messages)
        {
            Assert.DoesNotContain("secret-api-key-12345", message);
        }
    }

    [Fact]
    public async Task FindByImdbId_success_returns_and_caches()
    {
        var fetchCount = 0;
        var handler = new MockHandler(request =>
        {
            fetchCount++;
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("find/"))
            {
                return JsonResponse(new
                {
                    movie_results = new[]
                    {
                        new { id = 550, title = "Fight Club", original_title = "Fight Club", release_date = "1999-10-15", runtime = 139, imdb_id = "tt0137523" }
                    }
                });
            }

            if (uri.Contains("alternative_titles"))
            {
                return JsonResponse(new { titles = Array.Empty<object>() });
            }

            return JsonResponse(new { id = 550, title = "Fight Club", original_title = "Fight Club", release_date = "1999-10-15", runtime = 139, imdb_id = "tt0137523" });
        });
        var client = CreateClient(handler);

        var result = await client.FindByImdbIdAsync("tt0137523");

        Assert.NotNull(result);
        Assert.Equal("Fight Club", result.Movie.Title);
    }

    [Fact]
    public async Task FindByImdbId_not_found_returns_null()
    {
        var handler = new MockHandler(_ => JsonResponse(new { movie_results = Array.Empty<object>() }));
        var client = CreateClient(handler);

        var result = await client.FindByImdbIdAsync("tt9999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMovieDataAsync_cache_hit_skips_http()
    {
        var httpCalls = 0;
        var handler = new MockHandler(request =>
        {
            httpCalls++;
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("alternative_titles"))
            {
                return JsonResponse(new { titles = Array.Empty<object>() });
            }

            return JsonResponse(new { id = 550, title = "Fight Club", original_title = "Fight Club", release_date = "1999-10-15", runtime = 139, imdb_id = "tt0137523" });
        });
        var client = CreateClient(handler);

        await client.GetMovieDataAsync(550);
        var callsAfterFirst = httpCalls;

        await client.GetMovieDataAsync(550);
        Assert.Equal(callsAfterFirst, httpCalls);
    }

    [Fact]
    public async Task FindByImdbId_caches_then_GetMovieDataAsync_uses_cache()
    {
        var httpCalls = 0;
        var handler = new MockHandler(request =>
        {
            httpCalls++;
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("find/"))
            {
                return JsonResponse(new
                {
                    movie_results = new[]
                    {
                        new { id = 550, title = "Fight Club", original_title = "Fight Club", release_date = "1999-10-15", runtime = 139, imdb_id = "tt0137523" }
                    }
                });
            }

            if (uri.Contains("alternative_titles"))
            {
                return JsonResponse(new { titles = Array.Empty<object>() });
            }

            return JsonResponse(new { id = 550, title = "Fight Club", original_title = "Fight Club", release_date = "1999-10-15", runtime = 139, imdb_id = "tt0137523" });
        });
        var client = CreateClient(handler);

        await client.FindByImdbIdAsync("tt0137523");
        var callsAfterFind = httpCalls;

        var result = await client.GetMovieDataAsync(550);
        Assert.Equal(callsAfterFind, httpCalls);
        Assert.NotNull(result);
        Assert.Equal("Fight Club", result.Movie.Title);
    }

    private static HttpResponseMessage JsonResponse(object body)
    {
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

    private sealed class CapturingLogger : ILogger<TmdbClient>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
