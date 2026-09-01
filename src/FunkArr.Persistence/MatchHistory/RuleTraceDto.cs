using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class RuleTraceDto
{
    [JsonPropertyName("ruleId")]
    public string RuleId { get; init; } = "";

    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("outcome")]
    public string Outcome { get; init; } = "";

    [JsonPropertyName("filterTrace")]
    public FilterGroupTraceDto? FilterTrace { get; init; }

    [JsonPropertyName("identificationTrace")]
    public IdentificationTraceDto? IdentificationTrace { get; init; }
}
