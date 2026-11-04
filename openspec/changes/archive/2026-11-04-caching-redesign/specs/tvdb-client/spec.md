## MODIFIED Requirements

### Requirement: TvdbClient uses IMemoryCache

The TvdbClient SHALL accept `IMemoryCache` via constructor injection (replacing `IDistributedCache`) and use it for caching episode data. Cache entries SHALL use `AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)` and `Size = 1`. The cache key format SHALL remain `tvdb:episodes:{seriesId}`.

#### Scenario: Cache miss fetches from API

- **WHEN** `GetEpisodesAsync` is called for a series ID and no cache entry exists
- **THEN** the client SHALL fetch from the TVDB API, store the result in `IMemoryCache` with 7-day absolute expiration and Size = 1, and return it

#### Scenario: Cache hit returns stored object

- **WHEN** `GetEpisodesAsync` is called for a series ID and a cache entry exists
- **THEN** the client SHALL return the cached `TvdbEpisode[]` without making an HTTP request

#### Scenario: Fixed 7-day TTL replaces content-aware TTL

- **WHEN** a TVDB response is cached
- **THEN** the TTL SHALL be 7 days regardless of whether the show has upcoming episodes (replacing the previous 2-day/7-day dynamic TTL)

## REMOVED Requirements

### Requirement: TvdbClient uses IDistributedCache

**Reason**: Replaced by IMemoryCache — no JSON serialization overhead, built-in statistics, proper eviction.
**Migration**: Change constructor parameter from `IDistributedCache` to `IMemoryCache`. Replace `GetAsync<T>`/`SetAsync<T>` extension methods with `TryGetValue`/`Set`.
