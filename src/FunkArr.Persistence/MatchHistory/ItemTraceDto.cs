using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class ItemTraceDto
{
    [JsonPropertyName("candidateTitle")]
    public string CandidateTitle { get; init; } = "";

    [JsonPropertyName("candidateTopic")]
    public string CandidateTopic { get; init; } = "";

    [JsonPropertyName("candidateChannel")]
    public string CandidateChannel { get; init; } = "";

    [JsonPropertyName("candidateDuration")]
    public int CandidateDuration { get; init; }

    [JsonPropertyName("candidateQuality")]
    public int CandidateQuality { get; init; }

    [JsonPropertyName("candidateDescription")]
    public string? CandidateDescription { get; init; }

    [JsonPropertyName("candidateTimestamp")]
    public long CandidateTimestamp { get; init; }

    [JsonPropertyName("matched")]
    public bool Matched { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("matchedRuleId")]
    public string? MatchedRuleId { get; init; }

    [JsonPropertyName("identification")]
    public TracedIdentificationDto? Identification { get; init; }

    [JsonPropertyName("ruleTraces")]
    public RuleTraceDto[] RuleTraces { get; init; } = [];
}
