namespace FunkArr.Messages.MetadataResolver;

public sealed record CacheStatsResult(
    int TvdbEntries,
    int TmdbEntries,
    DateTimeOffset? OldestEntry);
