using System.Text.Json.Serialization;

namespace FunkArr.Search;

public sealed class TvdbClient(HttpClient httpClient)
{
    public async Task<TvdbShowInfo?> GetShowAsync(int tvdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"api/v2/series/{tvdbId}?language=de", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TvdbApiResponse<TvdbShowInfo>>(
                cancellationToken: cancellationToken);
            return result?.Data;
        }
        catch
        {
            return null;
        }
    }

    public async Task<TvdbEpisodeInfo[]?> GetEpisodesAsync(
        int tvdbId, int season, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"api/v2/series/{tvdbId}/episodes/query?airedSeason={season}&language=de",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TvdbApiResponse<TvdbEpisodeInfo[]>>(
                cancellationToken: cancellationToken);
            return result?.Data;
        }
        catch
        {
            return null;
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
