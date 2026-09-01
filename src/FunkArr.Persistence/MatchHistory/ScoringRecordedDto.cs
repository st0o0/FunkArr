using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class ScoringRecordedDto
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("requestId")]
    public string RequestId { get; init; } = "";

    [JsonPropertyName("source")]
    public string Source { get; init; } = "";

    [JsonPropertyName("query")]
    public string Query { get; init; } = "";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = "";

    [JsonPropertyName("candidateCount")]
    public int CandidateCount { get; init; }

    [JsonPropertyName("matchedCount")]
    public int MatchedCount { get; init; }

    [JsonPropertyName("itemTraces")]
    public ItemTraceDto[] ItemTraces { get; init; } = [];
}
