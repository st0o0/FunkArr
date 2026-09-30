## MODIFIED Requirements

### Requirement: Unified cache with content-aware TTLs
Each external API client (TmdbClient, TvdbClient) SHALL cache its own responses using `IDistributedCache` with content-aware TTLs. Cache keys SHALL use a `{provider}:{type}:{id}` pattern to ensure uniqueness across providers. The EnrichmentManager actor SHALL NOT own or manage the cache directly.

#### Scenario: TV show cache entry

- **WHEN** TVDB episode data for series 83214 is fetched
- **THEN** it SHALL be cached under key `tvdb:episodes:83214` with a content-aware TTL

#### Scenario: Movie cache entry

- **WHEN** TMDB movie data for movie 550 is fetched
- **THEN** it SHALL be cached under key `tmdb:movie:550` with a 30-day TTL

#### Scenario: Different providers same ID

- **WHEN** TVDB series 550 and TMDB movie 550 exist
- **THEN** they SHALL be stored as separate cache entries due to the provider prefix in the key

### Requirement: Cache invalidation
Cache entries SHALL be invalidated on TTL expiry (handled by the IDistributedCache backend). A `ClearCache` message to the EnrichmentManager SHALL trigger removal of cache entries via `IDistributedCache.RemoveAsync`.

#### Scenario: TTL expiry

- **WHEN** a cache entry has expired
- **THEN** the next `GetAsync<T>` call SHALL return null, triggering a fresh API fetch

#### Scenario: Manual cache clear

- **WHEN** `ClearCache` is received with no parameters
- **THEN** the EnrichmentManager SHALL remove all known cache keys from IDistributedCache

#### Scenario: Targeted cache clear

- **WHEN** `ClearCache("tvdb", 83214)` is received
- **THEN** only the cache entry for key `tvdb:episodes:83214` SHALL be removed

### Requirement: Cache stats query
The EnrichmentManager SHALL handle a `QueryCacheStats` message. Since IDistributedCache does not expose entry counts, the response SHALL report cache hit/miss counters from telemetry rather than entry counts.

#### Scenario: Cache stats response

- **WHEN** `QueryCacheStats` is received
- **THEN** the response SHALL include available cache statistics (hit count, miss count per provider)

#### Scenario: Empty cache stats

- **WHEN** `QueryCacheStats` is received and no cache operations have occurred
- **THEN** the response SHALL have zero counts
