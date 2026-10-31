using FunkArr.Messages.Enrichment;

namespace FunkArr.Search;

public sealed record MatchInfo(
    float Confidence,
    MatchMethod Method);
