namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedEnrichmentTrace(
    PersistedMatchMethod Method,
    float Confidence,
    bool Enriched,
    string? ResolvedSeason = null,
    string? ResolvedEpisode = null,
    string? ResolvedTitle = null,
    int? ResolvedYear = null,
    int? DaysDiff = null,
    string? Detail = null);
