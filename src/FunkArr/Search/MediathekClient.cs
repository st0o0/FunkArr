using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FunkArr.Search;

public sealed class MediathekClient(HttpClient httpClient, ILogger<MediathekClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly KeyValuePair<string, object?> ClientTag = new("client", "mediathek");
    private readonly Counter<long> _callTotal = FunkArrMetrics.Instance.AddApiCallTotal();
    private readonly Histogram<double> _callDuration = FunkArrMetrics.Instance.AddApiCallDuration();

    public async Task<MediathekQueryResponse?> QueryAsync(
        MediathekQuery query, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/query", query, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _callTotal.Add(1, ClientTag, new("outcome", "error"));
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("MediathekViewWeb response: {Length} bytes, starts with: {Start}",
                json.Length, json.Length > 100 ? json[..100] : json);
            var result = JsonSerializer.Deserialize<MediathekQueryResponse>(json, JsonOptions);
            logger.LogInformation("Deserialized: Result={HasResult}, Items={Count}",
                result?.Result is not null, result?.Result?.Results?.Length ?? 0);
            _callTotal.Add(1, ClientTag, new("outcome", "success"));
            return result;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON deserialization failed");
            _callTotal.Add(1, ClientTag, new("outcome", "error"));
            return null;
        }
        catch
        {
            _callTotal.Add(1, ClientTag, new("outcome", "error"));
            throw;
        }
        finally
        {
            _callDuration.Record(sw.Elapsed.TotalSeconds, ClientTag);
        }
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
    public MediathekQueryResult? Result { get; init; }
}

public sealed record MediathekQueryResult
{
    public MediathekResultItem[] Results { get; init; } = [];
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
    public long? Size { get; init; }
    public string Url_Website { get; init; } = string.Empty;
    public string Url_Video { get; init; } = string.Empty;
    public string Url_Video_Low { get; init; } = string.Empty;
    public string Url_Video_HD { get; init; } = string.Empty;
    public string Url_Subtitle { get; init; } = string.Empty;
}

public sealed record MediathekQueryMeta
{
    [System.Text.Json.Serialization.JsonConverter(typeof(FlexStringConverter))]
    public string FilmlisteTimestamp { get; init; } = string.Empty;
    public string SearchEngineTime { get; init; } = string.Empty;
    public int ResultCount { get; init; }
    public int TotalResults { get; init; }
}

internal sealed class FlexStringConverter : System.Text.Json.Serialization.JsonConverter<string>
{
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
        reader.TokenType == System.Text.Json.JsonTokenType.Number
            ? reader.GetInt64().ToString()
            : reader.GetString() ?? string.Empty;

    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
