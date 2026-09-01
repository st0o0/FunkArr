using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class FilterNodeTraceDto
{
    [JsonPropertyName("nodeType")]
    public string NodeType { get; init; } = "";

    [JsonPropertyName("field")]
    public string? Field { get; init; }

    [JsonPropertyName("op")]
    public string? Op { get; init; }

    [JsonPropertyName("expectedValue")]
    public string? ExpectedValue { get; init; }

    [JsonPropertyName("actualValue")]
    public string? ActualValue { get; init; }

    [JsonPropertyName("passed")]
    public bool Passed { get; init; }

    [JsonPropertyName("skipped")]
    public bool Skipped { get; init; }

    [JsonPropertyName("group")]
    public FilterGroupTraceDto? Group { get; init; }
}
