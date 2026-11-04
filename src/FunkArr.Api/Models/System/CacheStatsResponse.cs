namespace FunkArr.Api.Models;

public sealed record CacheStatsResponse(
    int TvdbEntries,
    int TmdbEntries,
    DateTimeOffset? OldestEntry);
