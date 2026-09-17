namespace FunkArr.Search;

public sealed record EnrichedItem(
    int Index,
    SourceInfo Source,
    double Score,
    bool Matched,
    bool HasScoringMetadata,
    MediaIdentity Identity,
    MatchInfo? Match);
