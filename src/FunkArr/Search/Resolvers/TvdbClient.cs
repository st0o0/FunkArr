using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using FunkArr.Diagnostics;

namespace FunkArr.Search.Resolvers;

public sealed class TvdbClient(HttpClient httpClient)
{
    private static readonly KeyValuePair<string, object?> ClientTag = new("client", "tvdb");
    private readonly Counter<long> _callTotal = FunkArrMetrics.Instance.AddApiCallTotal();
    private readonly Histogram<double> _callDuration = FunkArrMetrics.Instance.AddApiCallDuration();

    public async Task<TvdbShowInfo?> GetShowAsync(int tvdbId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await httpClient.GetAsync(
                $"api/v2/series/{tvdbId}?language=de", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _callTotal.Add(1, ClientTag, new("outcome", "error"));
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TvdbApiResponse<TvdbShowInfo>>(
                cancellationToken: cancellationToken);
            _callTotal.Add(1, ClientTag, new("outcome", "success"));
            return result?.Data;
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

    public async Task<TvdbEpisodeInfo[]?> GetEpisodesAsync(
        int tvdbId, int season, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await httpClient.GetAsync(
                $"api/v2/series/{tvdbId}/episodes/query?airedSeason={season}&language=de",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _callTotal.Add(1, ClientTag, new("outcome", "error"));
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TvdbApiResponse<TvdbEpisodeInfo[]>>(
                cancellationToken: cancellationToken);
            _callTotal.Add(1, ClientTag, new("outcome", "success"));
            return result?.Data;
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
}

public sealed record TvdbApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

public sealed record TvdbShowInfo
{
    [JsonPropertyName("seriesName")]
    public string SeriesName { get; init; } = string.Empty;

    [JsonPropertyName("aliases")]
    public string[] Aliases { get; init; } = [];

    [JsonPropertyName("overview")]
    public string Overview { get; init; } = string.Empty;
}

public sealed record TvdbEpisodeInfo
{
    [JsonPropertyName("episodeName")]
    public string EpisodeName { get; init; } = string.Empty;

    [JsonPropertyName("airedSeason")]
    public int AiredSeason { get; init; }

    [JsonPropertyName("airedEpisodeNumber")]
    public int AiredEpisodeNumber { get; init; }

    [JsonPropertyName("firstAired")]
    public string FirstAired { get; init; } = string.Empty;

    [JsonPropertyName("overview")]
    public string Overview { get; init; } = string.Empty;
}
