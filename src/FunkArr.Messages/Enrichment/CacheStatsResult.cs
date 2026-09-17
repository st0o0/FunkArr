namespace FunkArr.Messages.Enrichment;

public sealed record CacheStatsResult(
    int TvdbEntries,
    int TmdbEntries,
    DateTimeOffset? OldestEntry);
