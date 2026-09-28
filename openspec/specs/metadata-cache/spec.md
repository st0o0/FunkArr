# Metadata Cache

## Purpose

Distributed cache for metadata fetched from external providers (TVDB, TMDB), using content-aware TTLs to balance freshness against API rate limits. Cache ownership resides in individual clients (TmdbClient, TvdbClient), not the EnrichmentManager.

## Requirements

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

### Requirement: Tiered TTL based on content type
The cache SHALL apply different TTLs based on content type: active TV shows (2 days), inactive TV shows (7 days), movies (30 days), default (12 hours).

#### Scenario: Active show TTL
- **WHEN** a TV show has at least one TVDB episode with an aired date in the future
- **THEN** the cache entry SHALL use a 2-day TTL

#### Scenario: Inactive show TTL
- **WHEN** a TV show has no TVDB episodes with future aired dates
- **THEN** the cache entry SHALL use a 7-day TTL

#### Scenario: Movie TTL
- **WHEN** a TMDB movie entry is cached
- **THEN** the cache entry SHALL use a 30-day TTL

#### Scenario: Default TTL
- **WHEN** the content type cannot be determined
- **THEN** the cache entry SHALL use a 12-hour TTL

### Requirement: Active show detection
A TV show SHALL be considered active if any of its TVDB episodes have an `aired` date that is in the future or within the last 30 days. Otherwise it SHALL be considered inactive.

#### Scenario: Show with upcoming episode
- **WHEN** a series has an episode with aired date 2 weeks from now
- **THEN** the show SHALL be classified as active (2-day TTL)

#### Scenario: Show with no recent episodes
- **WHEN** a series has no episodes aired within the last 30 days and none in the future
- **THEN** the show SHALL be classified as inactive (7-day TTL)

### Requirement: Cache stats query
The EnrichmentManager SHALL handle a `QueryCacheStats` message. Since IDistributedCache does not expose entry counts, the response SHALL report cache hit/miss counters from telemetry rather than entry counts.

#### Scenario: Cache stats response
- **WHEN** `QueryCacheStats` is received
- **THEN** the response SHALL include available cache statistics (hit count, miss count per provider)

#### Scenario: Empty cache stats
- **WHEN** `QueryCacheStats` is received and no cache operations have occurred
- **THEN** the response SHALL have zero counts

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
