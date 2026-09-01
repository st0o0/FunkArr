using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class IdentificationTraceDto
{
    [JsonPropertyName("strategy")]
    public string? Strategy { get; init; }

    [JsonPropertyName("attempted")]
    public bool Attempted { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }
}
