## ADDED Requirements

### Requirement: IMemoryCache registration in CoreSetupContainer

CoreSetupContainer SHALL register a shared `IMemoryCache` instance via `services.AddMemoryCache(options => ...)` with `SizeLimit = 2048` (entry count), `TrackStatistics = true`, and default `CompactionPercentage` (0.25).

This replaces the previous `services.AddDistributedMemoryCache()` registration.

#### Scenario: Registration configures size limit and statistics

- **WHEN** CoreSetupContainer registers caching services
- **THEN** `IMemoryCache` SHALL be registered with `SizeLimit = 2048` and `TrackStatistics = true`

#### Scenario: Single shared instance across all consumers

- **WHEN** TvdbClient, TmdbClient, MediathekClient, or NewznabSearchService resolve `IMemoryCache`
- **THEN** they SHALL all receive the same singleton instance

### Requirement: Cache entries specify Size

Every cache entry created via `IMemoryCache` SHALL set `MemoryCacheEntryOptions.Size = 1` (one unit per entry). This is required when `SizeLimit` is configured — entries without a size are rejected.

#### Scenario: Entry without Size is rejected

- **WHEN** a consumer creates a cache entry without setting `Size`
- **THEN** the entry SHALL not be stored and an error is logged by the framework

#### Scenario: All consumers set Size = 1

- **WHEN** TvdbClient, TmdbClient, MediathekClient, or NewznabSearchService create a cache entry
- **THEN** the entry SHALL have `Size = 1`

### Requirement: No JSON serialization for cache entries

Cache entries SHALL store .NET object references directly. The `DistributedCacheExtensions` class (JSON serialization to/from byte arrays) SHALL be deleted. `IMemoryCache` is in-process only — serialization is unnecessary overhead.

#### Scenario: Object stored and retrieved without serialization

- **WHEN** a `TvdbEpisode[]` is cached and later retrieved
- **THEN** the returned reference SHALL be the same object instance (reference equality), not a deserialized copy
