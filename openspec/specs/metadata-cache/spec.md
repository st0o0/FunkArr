# Metadata Cache

## Purpose

In-memory cache for metadata fetched from external providers (TVDB, TMDB), with fixed TTLs and built-in eviction. Cache ownership resides in individual clients (TmdbClient, TvdbClient), not the EnrichmentManager.

## Requirements

### Requirement: Metadata caching uses IMemoryCache

Metadata caching for TVDB and TMDB data SHALL use `IMemoryCache` (replacing `IDistributedCache`). Cache ownership remains in the individual clients (TvdbClient, TmdbClient), not in EnrichmentManager. Cache keys SHALL use a `{provider}:{type}:{id}` pattern to ensure uniqueness across providers.

#### Scenario: Cache is in-process with no serialization

- **WHEN** metadata is cached
- **THEN** the cache SHALL store .NET object references directly via `IMemoryCache`, not serialized byte arrays

#### Scenario: Cache entries are evictable

- **WHEN** the cache reaches its size limit (2048 entries)
- **THEN** the framework SHALL evict entries based on priority and expiration (compaction)

#### Scenario: TV show cache entry
- **WHEN** TVDB episode data for series 83214 is fetched
- **THEN** it SHALL be cached under key `tvdb:episodes:83214` with a 7-day TTL

#### Scenario: Movie cache entry
- **WHEN** TMDB movie data for movie 550 is fetched
- **THEN** it SHALL be cached under key `tmdb:movie:550` with a 7-day TTL

#### Scenario: Different providers same ID
- **WHEN** TVDB series 550 and TMDB movie 550 exist
- **THEN** they SHALL be stored as separate cache entries due to the provider prefix in the key
