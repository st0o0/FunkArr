namespace FunkArr.Messages.Scoring;

public sealed record MetadataSpec(
    string? Season,
    string? Episode,
    DateTimeOffset? AiredAt);
