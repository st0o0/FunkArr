namespace FunkArr.MatchMagic;

public sealed record MatchResult(
    MediaItem Item,
    Rule MatchedRule,
    EpisodeIdentification Identification,
    string? ConstructedTitle,
    float Confidence,
    IReadOnlyList<QualityVariant> Qualities);
