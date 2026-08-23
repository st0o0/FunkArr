using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using FunkArr.Configuration;
using FunkArr.Diagnostics;
using Microsoft.Extensions.Options;

namespace FunkArr.Search;

public sealed class TmdbClient(HttpClient httpClient, IOptions<SearchOptions> options)
{
    private readonly string? _apiKey = options.Value.TmdbApiKey;
    private static readonly KeyValuePair<string, object?> ClientTag = new("client", "tmdb");
    private readonly Counter<long> _callTotal = FunkArrMetrics.Instance.AddApiCallTotal();
    private readonly Histogram<double> _callDuration = FunkArrMetrics.Instance.AddApiCallDuration();

    public async Task<TmdbMovieInfo?> FindByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await httpClient.GetAsync(
                $"find/{imdbId}?api_key={_apiKey}&external_source=imdb_id&language=de-DE",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _callTotal.Add(1, ClientTag, new("outcome", "error"));
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbFindResponse>(
                cancellationToken: cancellationToken);

            var movie = result?.MovieResults?.FirstOrDefault();
            if (movie is null)
            {
                _callTotal.Add(1, ClientTag, new("outcome", "success"));
                return null;
            }

            var runtime = await GetRuntimeAsync(movie.Id, cancellationToken);

            _callTotal.Add(1, ClientTag, new("outcome", "success"));
            return new TmdbMovieInfo
            {
                Title = movie.Title,
                OriginalTitle = movie.OriginalTitle,
                ReleaseYear = ParseYear(movie.ReleaseDate),
                RuntimeMinutes = runtime,
            };
        }
        catch
        {
            _callTotal.Add(1, ClientTag, new("outcome", "error"));
            return null;
        }
        finally
        {
            _callDuration.Record(sw.Elapsed.TotalSeconds, ClientTag);
        }
    }

    public async Task<TmdbMovieInfo?> SearchMovieAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await httpClient.GetAsync(
                $"search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&language=de-DE",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _callTotal.Add(1, ClientTag, new("outcome", "error"));
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbSearchResponse>(
                cancellationToken: cancellationToken);

            var movie = result?.Results?.FirstOrDefault();
            if (movie is null)
            {
                _callTotal.Add(1, ClientTag, new("outcome", "success"));
                return null;
            }

            var runtime = await GetRuntimeAsync(movie.Id, cancellationToken);

            _callTotal.Add(1, ClientTag, new("outcome", "success"));
            return new TmdbMovieInfo
            {
                Title = movie.Title,
                OriginalTitle = movie.OriginalTitle,
                ReleaseYear = ParseYear(movie.ReleaseDate),
                RuntimeMinutes = runtime,
            };
        }
        catch
        {
            _callTotal.Add(1, ClientTag, new("outcome", "error"));
            return null;
        }
        finally
        {
            _callDuration.Record(sw.Elapsed.TotalSeconds, ClientTag);
        }
    }

    private async Task<int?> GetRuntimeAsync(int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"movie/{tmdbId}?api_key={_apiKey}&language=de-DE",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var detail = await response.Content.ReadFromJsonAsync<TmdbMovieDetail>(
                cancellationToken: cancellationToken);

            return detail?.Runtime is > 0 ? detail.Runtime : null;
        }
        catch
        {
            return null;
        }
    }

    private static int? ParseYear(string? releaseDate)
    {
        if (releaseDate is { Length: >= 4 } && int.TryParse(releaseDate[..4], out var year))
        {
            return year;
        }

        return null;
    }
}

public sealed record TmdbMovieInfo
{
    public string Title { get; init; } = string.Empty;
    public string OriginalTitle { get; init; } = string.Empty;
    public int? ReleaseYear { get; init; }
    public int? RuntimeMinutes { get; init; }
}

internal sealed record TmdbFindResponse
{
    [JsonPropertyName("movie_results")]
    public TmdbMovieResult[]? MovieResults { get; init; }
}

internal sealed record TmdbSearchResponse
{
    [JsonPropertyName("results")]
    public TmdbMovieResult[]? Results { get; init; }
}

internal sealed record TmdbMovieResult
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; init; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; init; }
}

internal sealed record TmdbMovieDetail
{
    [JsonPropertyName("runtime")]
    public int? Runtime { get; init; }
}
