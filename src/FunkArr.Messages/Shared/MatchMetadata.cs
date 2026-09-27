using FunkArr.Messages.Enrichment;

namespace FunkArr.Messages.Shared;

public sealed record MatchMetadata(
    ExternalIds? Ids,
    string? Season,
    string? Episode,
    float? MatchConfidence,
    MatchMethod? MatchMethod);
