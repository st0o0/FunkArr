## MODIFIED Requirements

### Requirement: TvdbOptions configuration
The system SHALL define a `TvdbOptions` class bound to the `FunkArr:Tvdb` configuration section. It SHALL expose `ApiKey` (string, nullable) for the TVDB v4 API key. When `ApiKey` is null or empty, the TVDB client SHALL be disabled and all lookups SHALL return empty results. The class SHALL reside in `FunkArr.Core` namespace.

#### Scenario: API key configured
- **WHEN** `FunkArr__Tvdb__ApiKey` environment variable is set to a valid key
- **THEN** the TvdbOptions.ApiKey property SHALL contain that key

#### Scenario: No API key configured
- **WHEN** no TVDB API key is configured
- **THEN** TvdbOptions.ApiKey SHALL be null and the TVDB client SHALL skip all API calls

### Requirement: Episode data caching
The TVDB client SHALL cache episode data per series ID. Cache entries SHALL use content-aware TTLs: active shows (with upcoming or recent episodes) SHALL use a 2-day TTL, inactive shows SHALL use a 7-day TTL. The default TTL of 12 hours SHALL remain as fallback. Cache SHALL be stored in-memory.

#### Scenario: Active show cache
- **WHEN** episodes for a series with a future episode airdate are fetched
- **THEN** they SHALL be cached with a 2-day TTL

#### Scenario: Inactive show cache
- **WHEN** episodes for a series with no recent or future airdates are fetched
- **THEN** they SHALL be cached with a 7-day TTL

#### Scenario: Cache hit within TTL
- **WHEN** episodes for series 83214 were fetched within the TTL
- **THEN** a subsequent request for series 83214 SHALL return cached data without an API call

#### Scenario: Cache miss after TTL
- **WHEN** a cache entry has expired
- **THEN** a subsequent request SHALL fetch fresh data from the TVDB API

### Requirement: TvdbClient namespace
The TvdbClient class and TvdbEpisode record SHALL reside in the `FunkArr.MetadataResolver` namespace (renamed from `FunkArr.EpisodeGuide`).

#### Scenario: Namespace
- **WHEN** TvdbClient is referenced
- **THEN** it SHALL be in the `FunkArr.MetadataResolver` namespace
