## MODIFIED Requirements

### Requirement: TVDB response caching
The TvdbClient SHALL cache episode data using `IDistributedCache` (injected via constructor) with the typed extension methods from `DistributedCacheExtensions`. Cache keys SHALL follow the pattern `tvdb:episodes:{id}`. Episode entries SHALL use content-aware TTLs: 2-day for active shows (episodes with future or recent aired dates), 7-day for inactive shows.

#### Scenario: Cache hit

- **WHEN** `GetEpisodesAsync` is called for a series ID that exists in the distributed cache
- **THEN** the client SHALL return the cached `TvdbEpisode[]` without making an API call

#### Scenario: Cache miss with active show

- **WHEN** `GetEpisodesAsync` is called for a series ID not in cache, and the fetched episodes include future aired dates
- **THEN** the client SHALL store the result with a 2-day TTL

#### Scenario: Cache miss with inactive show

- **WHEN** `GetEpisodesAsync` is called for a series ID not in cache, and no episodes have future aired dates
- **THEN** the client SHALL store the result with a 7-day TTL

#### Scenario: Disabled client cache behavior

- **WHEN** no TVDB API key is configured and `GetEpisodesAsync` is called
- **THEN** the client SHALL return an empty array without accessing the cache
