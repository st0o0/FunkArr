using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class FilterGroupTraceDto
{
    [JsonPropertyName("operator")]
    public string Operator { get; init; } = "";

    [JsonPropertyName("passed")]
    public bool Passed { get; init; }

    [JsonPropertyName("nodes")]
    public FilterNodeTraceDto[] Nodes { get; init; } = [];
}
