using System.Text.Json.Serialization;

namespace FunkArr.Persistence.MatchHistory;

public sealed class TracedIdentificationDto
{
    [JsonPropertyName("season")]
    public string? Season { get; init; }

    [JsonPropertyName("episode")]
    public string? Episode { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}
