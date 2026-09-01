using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class MatchHistorySnapshotDto
{
    [JsonPropertyName("snapshots")]
    public SnapshotEntryDto[] Snapshots { get; init; } = [];

    public sealed class SnapshotEntryDto
    {
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
}
