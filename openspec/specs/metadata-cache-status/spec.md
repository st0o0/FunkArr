# metadata-cache-status Specification

## Purpose
TBD - created by archiving change dashboard-richness. Update Purpose after archive.
## Requirements
### Requirement: Cache stats endpoint
The system SHALL respond to `GET /api/health/cache` with TMDB and TVDB cache statistics from the MetadataResolverManager.

#### Scenario: Cache stats available
- **WHEN** `GET /api/health/cache` is requested
- **THEN** the response SHALL be JSON with `tvdbEntries` (int), `tmdbEntries` (int), `oldestEntry` (ISO 8601 string or null)

#### Scenario: Actor timeout
- **WHEN** the MetadataResolverManager does not respond within 10 seconds
- **THEN** the response SHALL be HTTP 504 Gateway Timeout

### Requirement: Dashboard cache info
The Dashboard SHALL display metadata cache statistics.

#### Scenario: Cache stats displayed
- **WHEN** the Dashboard loads and cache stats are available
- **THEN** a compact info section SHALL show TVDB entry count, TMDB entry count, and oldest entry age

#### Scenario: No cache entries
- **WHEN** both TVDB and TMDB caches are empty
- **THEN** the cache section SHALL show "0" for both counts and no oldest entry

