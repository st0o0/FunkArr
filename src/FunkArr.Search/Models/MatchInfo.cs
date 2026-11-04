using FunkArr.Messages.Enrichment;

namespace FunkArr.Search.Models;

public sealed record MatchInfo(
    float Confidence,
    MatchMethod Method);
