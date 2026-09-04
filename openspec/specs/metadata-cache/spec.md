# Metadata Cache

## Purpose

In-memory cache for metadata fetched from external providers (TVDB, TMDB), using content-aware TTLs to balance freshness against API rate limits. Owned by the MetadataResolver actor.

## Requirements

### Requirement: Unified cache with content-aware TTLs
The MetadataResolver SHALL maintain a unified in-memory cache keyed by (provider, id) tuples — e.g., ("tvdb", 83214) or ("tmdb", 550). Cache entries SHALL have content-aware TTLs based on content type.

#### Scenario: TV show cache entry
- **WHEN** TVDB episode data for series 83214 is fetched
- **THEN** it SHALL be cached under key ("tvdb", 83214) with a content-aware TTL

#### Scenario: Movie cache entry
- **WHEN** TMDB movie data for movie 550 is fetched
- **THEN** it SHALL be cached under key ("tmdb", 550) with a 30-day TTL

#### Scenario: Different providers same ID
- **WHEN** TVDB series 550 and TMDB movie 550 exist
- **THEN** they SHALL be stored as separate cache entries due to the (provider, id) composite key

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
The MetadataResolver SHALL handle a `QueryCacheStats` message and respond with a `CacheStatsResult` containing: TotalEntries (int), TvdbEntries (int), TmdbEntries (int), OldestEntry (DateTimeOffset?), NewestEntry (DateTimeOffset?).

#### Scenario: Cache stats response
- **WHEN** `QueryCacheStats` is received and the cache has 5 TVDB and 2 TMDB entries
- **THEN** the response SHALL have TotalEntries=7, TvdbEntries=5, TmdbEntries=2

#### Scenario: Empty cache stats
- **WHEN** `QueryCacheStats` is received and the cache is empty
- **THEN** the response SHALL have TotalEntries=0

### Requirement: Cache invalidation
Cache entries SHALL be invalidated on TTL expiry (lazy eviction on next access). A `ClearCache` message SHALL force-clear all entries or entries for a specific provider/id.

#### Scenario: TTL expiry
- **WHEN** a cache entry has expired
- **THEN** the next access SHALL trigger a fresh API fetch

#### Scenario: Manual cache clear
- **WHEN** `ClearCache` is received with no parameters
- **THEN** all cache entries SHALL be removed

#### Scenario: Targeted cache clear
- **WHEN** `ClearCache("tvdb", 83214)` is received
- **THEN** only the cache entry for TVDB series 83214 SHALL be removed
