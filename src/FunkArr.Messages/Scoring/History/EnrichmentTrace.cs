using FunkArr.Messages.Enrichment;

namespace FunkArr.Messages.Scoring.History;

public sealed record EnrichmentTrace(
    MatchMethod Method,
    float Confidence,
    bool Enriched,
    string? ResolvedSeason = null,
    string? ResolvedEpisode = null,
    string? ResolvedTitle = null,
    int? ResolvedYear = null,
    int? DaysDiff = null,
    string? Detail = null);
