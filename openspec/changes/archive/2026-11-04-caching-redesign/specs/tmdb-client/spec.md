## MODIFIED Requirements

### Requirement: TmdbClient uses IMemoryCache

The TmdbClient SHALL accept `IMemoryCache` via constructor injection (replacing `IDistributedCache`) and use it for caching movie data. Cache entries SHALL use `AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)` and `Size = 1`. The cache key format SHALL remain `tmdb:movie:{tmdbId}`.

#### Scenario: Cache miss fetches from API

- **WHEN** `GetMovieAsync` is called for a TMDB ID and no cache entry exists
- **THEN** the client SHALL fetch from the TMDB API, store the `TmdbMovieData` in `IMemoryCache` with 7-day absolute expiration and Size = 1, and return it

#### Scenario: Cache hit returns stored object

- **WHEN** `GetMovieAsync` is called for a TMDB ID and a cache entry exists
- **THEN** the client SHALL return the cached `TmdbMovieData` without making an HTTP request

#### Scenario: FindByImdbId checks cache after ID resolution

- **WHEN** `FindByImdbIdAsync` resolves an IMDB ID to a TMDB ID
- **THEN** the client SHALL check `IMemoryCache` for key `tmdb:movie:{tmdbId}` before making a second API call

#### Scenario: TTL reduced to 7 days

- **WHEN** a TMDB response is cached
- **THEN** the TTL SHALL be 7 days (reduced from previous 30 days)

## REMOVED Requirements

### Requirement: TmdbClient uses IDistributedCache

**Reason**: Replaced by IMemoryCache — no JSON serialization overhead, built-in statistics, proper eviction.
**Migration**: Change constructor parameter from `IDistributedCache` to `IMemoryCache`. Replace `GetAsync<T>`/`SetAsync<T>` extension methods with `TryGetValue`/`Set`.
