## MODIFIED Requirements

### Requirement: TMDB response caching
The TmdbClient SHALL cache movie data using `IDistributedCache` (injected via constructor) with the typed extension methods from `DistributedCacheExtensions`. Cache keys SHALL follow the pattern `tmdb:movie:{id}`. Movie entries SHALL use a 30-day absolute expiration.

#### Scenario: Cache hit

- **WHEN** `GetMovieAsync` is called for a movie ID that exists in the distributed cache
- **THEN** the client SHALL return the cached `TmdbMovieData` without making an API call

#### Scenario: Cache miss

- **WHEN** `GetMovieAsync` is called for a movie ID not in the distributed cache
- **THEN** the client SHALL fetch from the API and store the result in the distributed cache with a 30-day TTL

#### Scenario: Disabled client cache behavior

- **WHEN** no TMDB API key is configured and `GetMovieAsync` is called
- **THEN** the client SHALL return null without accessing the cache
