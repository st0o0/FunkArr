using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.MetadataResolver;

public sealed class TmdbClient(HttpClient httpClient, IOptionsMonitor<TmdbOptions> options, IMemoryCache cache, ILogger<TmdbClient> log)
{
    private static readonly TimeSpan _movieTtl = TimeSpan.FromDays(30);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public bool IsConfigured => !string.IsNullOrEmpty(options.CurrentValue.ApiKey);

    public async Task<TmdbMovieData?> GetMovieDataAsync(int tmdbId)
    {
        var cacheKey = $"tmdb:movie:{tmdbId}";
        if (cache.TryGetValue(cacheKey, out TmdbMovieData? cached))
        {
            log.LogDebug("TMDB cache hit for movie {TmdbId}", tmdbId);
            return cached;
        }

        log.LogDebug("TMDB cache miss for movie {TmdbId}, fetching", tmdbId);
        var movie = await FetchMovieAsync(tmdbId);
        if (movie is null)
        {
            return null;
        }

        var altTitles = await FetchAlternativeTitlesAsync(tmdbId);
        var data = new TmdbMovieData(movie, altTitles);
        cache.Set(cacheKey, data, _movieTtl);
        return data;
    }

    public async Task<TmdbMovieData?> FindByImdbIdAsync(string imdbId)
    {
        var url = $"find/{imdbId}?api_key={ApiKey()}&external_source=imdb_id";
        var result = await FetchAsync<TmdbFindResponse>(url);
        var movie = result?.MovieResults is { Length: > 0 } ? result.MovieResults[0] : null;

        if (movie is null)
        {
            return null;
        }

        var cacheKey = $"tmdb:movie:{movie.Id}";
        if (cache.TryGetValue(cacheKey, out TmdbMovieData? cached))
        {
            return cached;
        }

        var altTitles = await FetchAlternativeTitlesAsync(movie.Id);
        var data = new TmdbMovieData(movie, altTitles);
        cache.Set(cacheKey, data, _movieTtl);
        return data;
    }

    public int CacheEntryCount => (cache as MemoryCache)?.Count ?? 0;

    private async Task<TmdbMovie?> FetchMovieAsync(int tmdbId)
    {
        var url = $"movie/{tmdbId}?api_key={ApiKey()}";
        return await FetchAsync<TmdbMovie>(url);
    }

    private async Task<string[]> FetchAlternativeTitlesAsync(int tmdbId)
    {
        var url = $"movie/{tmdbId}/alternative_titles?api_key={ApiKey()}";
        var result = await FetchAsync<TmdbAlternativeTitlesResponse>(url);
        return result?.Titles?.Select(t => t.Title).Where(t => t is not null).Cast<string>().ToArray() ?? [];
    }

    private string ApiKey()
    {
        var key = options.CurrentValue.ApiKey;
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("TMDB API key is not configured");
        }

        return key;
    }

    private async Task<T?> FetchAsync<T>(string url) where T : class
    {
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("TMDB request failed: {StatusCode} for {Url}", (int)response.StatusCode, url);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }
}

public sealed record TmdbMovie(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("original_title")] string? OriginalTitle,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("runtime")] int? Runtime,
    [property: JsonPropertyName("imdb_id")] string? ImdbId);

public sealed record TmdbMovieData(TmdbMovie Movie, string[] AltTitles);

file sealed class TmdbFindResponse
{
    [JsonPropertyName("movie_results")]
    public TmdbMovie[]? MovieResults { get; set; }
}

file sealed class TmdbAlternativeTitlesResponse
{
    [JsonPropertyName("titles")]
    public TmdbAlternativeTitle[]? Titles { get; set; }
}

file sealed class TmdbAlternativeTitle
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
