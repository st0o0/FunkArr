## MODIFIED Requirements

### Requirement: Unified cache with content-aware TTLs
The MetadataResolverManager SHALL maintain a unified in-memory cache of raw API responses keyed by (provider, id) tuples - e.g., ("tvdb", 83214) or ("tmdb", 550). Cache entries SHALL store raw provider data (TvdbEpisode[] for TVDB, TmdbMovie + alternative titles for TMDB) and SHALL have content-aware TTLs based on content type. Resolution SHALL NOT be cached - it SHALL be recomputed on every request using the cached raw data and the current request parameters.

#### Scenario: TV show cache entry stores raw episodes
- **WHEN** TVDB episode data for series 83214 is fetched
- **THEN** it SHALL be cached under key ("tvdb", 83214) with the raw TvdbEpisode[] array and a content-aware TTL

#### Scenario: Movie cache entry stores raw movie data
- **WHEN** TMDB movie data for movie 550 is fetched
- **THEN** it SHALL be cached under key ("tmdb", 550) with the TmdbMovie and alternative titles and a 30-day TTL

#### Scenario: Cache hit re-resolves with current parameters
- **WHEN** a ResolveEpisodes request hits the cache for series 83214
- **THEN** the Manager SHALL filter the cached TvdbEpisode[] by the requested Season, resolve against the provided Candidates and Config, and return freshly computed ResolvedEpisode[]

#### Scenario: Same series different season returns correct results
- **WHEN** Season 1 was requested for series 83214, then Season 2 is requested for the same series
- **THEN** both requests SHALL return correctly resolved results for their respective seasons using the same cached episode data

#### Scenario: Same series different candidates returns correct results
- **WHEN** series 83214 is requested with Candidates A, then with Candidates B
- **THEN** each request SHALL return results resolved against its own Candidates

#### Scenario: Different providers same ID
- **WHEN** TVDB series 550 and TMDB movie 550 exist
- **THEN** they SHALL be stored as separate cache entries due to the (provider, id) composite key

### Requirement: Cache invalidation
Cache entries SHALL be invalidated on TTL expiry via both lazy eviction (on next access) and active eviction (periodic timer). A `ClearCache` message SHALL force-clear all entries or entries for a specific provider/id.

#### Scenario: TTL expiry on access
- **WHEN** a cache entry has expired and is accessed
- **THEN** the access SHALL trigger a fresh API fetch

#### Scenario: Periodic eviction
- **WHEN** the eviction timer fires
- **THEN** all expired cache entries SHALL be removed from both caches

#### Scenario: Manual cache clear
- **WHEN** `ClearCache` is received with no parameters
- **THEN** all cache entries SHALL be removed

#### Scenario: Targeted cache clear
- **WHEN** `ClearCache("tvdb", 83214)` is received
- **THEN** only the cache entry for TVDB series 83214 SHALL be removed

### Requirement: Cache stats query
The MetadataResolverManager SHALL handle a `QueryCacheStats` message and respond with a `CacheStatsResult` containing: TotalEntries (int), TvdbEntries (int), TmdbEntries (int), OldestEntry (DateTimeOffset?), NewestEntry (DateTimeOffset?).

#### Scenario: Cache stats response
- **WHEN** `QueryCacheStats` is received and the cache has 5 TVDB and 2 TMDB entries
- **THEN** the response SHALL have TotalEntries=7, TvdbEntries=5, TmdbEntries=2

#### Scenario: Empty cache stats
- **WHEN** `QueryCacheStats` is received and the cache is empty
- **THEN** the response SHALL have TotalEntries=0

## ADDED Requirements

### Requirement: Concurrent request deduplication
When multiple callers request the same provider ID concurrently and a fetch is already in-flight, the Manager SHALL NOT send duplicate fetch requests. Instead it SHALL queue the additional callers and resolve for each individually when the fetch completes.

#### Scenario: Two concurrent requests for same series
- **WHEN** caller A requests series 83214 (cache miss, fetch dispatched) and caller B requests series 83214 before the fetch returns
- **THEN** only one fetch SHALL be sent to the TVDB pool, and both callers SHALL receive individually resolved responses when the fetch completes

#### Scenario: Concurrent requests with different parameters
- **WHEN** caller A requests series 83214 Season 1 and caller B requests series 83214 Season 2 concurrently
- **THEN** one fetch SHALL retrieve all episodes, and each caller SHALL receive results resolved for their requested season

#### Scenario: Fetch failure with pending requests
- **WHEN** a fetch fails and multiple callers are pending for that ID
- **THEN** all pending callers SHALL receive the failure response

### Requirement: Pool actors are pure fetch actors
TvdbResolverActor and TmdbResolverActor SHALL only fetch data from their respective APIs. They SHALL NOT receive Candidates, Config, or perform resolution. They SHALL return raw API data to the Manager.

#### Scenario: TVDB pool returns raw episodes
- **WHEN** TvdbResolverActor fetches episodes for series 83214
- **THEN** it SHALL send a TvdbFetchResult(TvdbId, TvdbEpisode[]) to the Manager

#### Scenario: TMDB pool returns raw movie data
- **WHEN** TmdbResolverActor fetches movie 550
- **THEN** it SHALL send a TmdbFetchResult(TmdbId, TmdbMovie, AltTitles) to the Manager

#### Scenario: Pool actor fetch failure
- **WHEN** an API call fails in a pool actor
- **THEN** the pool actor SHALL send a typed failure message to the Manager (not to the original Sender)
