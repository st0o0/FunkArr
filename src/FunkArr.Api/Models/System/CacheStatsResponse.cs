namespace FunkArr.Api.Models.System;

public sealed record CacheStatsResponse(
    int TvdbEntries,
    int TmdbEntries,
    DateTimeOffset? OldestEntry);
