using FunkArr.Messages.Enrichment;

namespace FunkArr.Feed;

public sealed record MatchInfo(
    float Confidence,
    MatchMethod Method);
