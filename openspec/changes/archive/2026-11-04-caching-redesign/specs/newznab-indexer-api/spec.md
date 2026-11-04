## MODIFIED Requirements

### Requirement: Newznab search results are cached via IMemoryCache

NewznabSearchService SHALL use `IMemoryCache` for caching search results (replacing the custom `SearchResultCache` class). Cache entries SHALL use `AbsoluteExpirationRelativeToNow` from `ArrApiOptions.SearchCacheTtlSeconds` (default 60s) and `Size = 1`.

#### Scenario: Cache miss executes search and caches result

- **WHEN** a Newznab search request arrives and no cache entry exists for the derived key
- **THEN** the service SHALL execute the search via actor Ask, store the result in `IMemoryCache`, and return it

#### Scenario: Cache hit returns stored result

- **WHEN** a Newznab search request arrives and a valid cache entry exists
- **THEN** the service SHALL return the cached result without executing a search

#### Scenario: Cache key format

- **WHEN** a search request is processed
- **THEN** the cache key SHALL follow the existing patterns: `tv:{query}:{tvdbId}:{imdbId}`, `movie:{query}:{imdbId}:{tmdbId}`, `general:{query}:{cat}`

#### Scenario: Concurrent identical requests are deduplicated

- **WHEN** multiple identical search requests arrive concurrently before the first completes
- **THEN** only one search SHALL be executed, and all requests SHALL receive the same result

### Requirement: OutputCache NewznabFeed TTL reduced to 5 minutes

The NewznabFeed OutputCache policy SHALL use a duration of 5 minutes (reduced from 30 minutes). No active invalidation — TTL-only expiry.

#### Scenario: Feed response cached for 5 minutes

- **WHEN** a Newznab feed-mode request is served
- **THEN** the OutputCache SHALL cache the response for 5 minutes

#### Scenario: No active cache invalidation

- **WHEN** FeedStoreManager persists new feed entries
- **THEN** no OutputCache invalidation SHALL occur — the cache expires naturally via TTL

## REMOVED Requirements

### Requirement: SearchResultCache class

**Reason**: Replaced by `IMemoryCache`. The custom `ConcurrentDictionary`-based cache had a memory leak (entries were never evicted, only soft-expired on read). `IMemoryCache` provides built-in eviction, size limits, and statistics.
**Migration**: Delete `SearchResultCache.cs`. Delete its DI registration. Update `NewznabSearchService` to inject `IMemoryCache` directly.

### Requirement: IFeedCacheInvalidator cross-domain interface

**Reason**: The 5-minute OutputCache TTL eliminates the need for active invalidation. The interface in Core violated domain boundaries (Core knowing about HTTP caching). Prowlarr polls every 15-60 minutes — 5-minute cache lag is irrelevant.
**Migration**: Delete `IFeedCacheInvalidator` from Core. Delete `OutputCacheFeedInvalidator` from ArrApi. Remove `InvalidateCache()` method, `CacheInvalidated`/`CacheInvalidationFailed` handling from FeedStoreManager. Remove `IFeedCacheInvalidator` constructor parameter from FeedStoreManager.
