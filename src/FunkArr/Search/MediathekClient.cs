using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunkArr.Search;

public sealed class MediathekClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<MediathekQueryResponse?> QueryAsync(
        MediathekQuery query, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/query", query, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MediathekQueryResponse>(
            JsonOptions, cancellationToken);
    }
}

public sealed record MediathekQuery
{
    public required MediathekQueryItem[] Queries { get; init; }
    public string SortBy { get; init; } = "timestamp";
    public string SortOrder { get; init; } = "desc";
    public int Offset { get; init; }
    public int Size { get; init; } = 5000;
}

public sealed record MediathekQueryItem
{
    public required string[] Fields { get; init; }
    public required string Query { get; init; }
}

public sealed record MediathekQueryResponse
{
    public MediathekResultItem[] Result { get; init; } = [];
    public MediathekQueryMeta? QueryInfo { get; init; }
}

public sealed record MediathekResultItem
{
    public string Channel { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public long Timestamp { get; init; }
    public int Duration { get; init; }
    public long Size { get; init; }
    public string Url_Website { get; init; } = string.Empty;
    public string Url_Video { get; init; } = string.Empty;
    public string Url_Video_Low { get; init; } = string.Empty;
    public string Url_Video_HD { get; init; } = string.Empty;
    public string Url_Subtitle { get; init; } = string.Empty;
}

public sealed record MediathekQueryMeta
{
    public string FilmlisteTimestamp { get; init; } = string.Empty;
    public string SearchEngineTime { get; init; } = string.Empty;
    public int ResultCount { get; init; }
    public int TotalResults { get; init; }
}
