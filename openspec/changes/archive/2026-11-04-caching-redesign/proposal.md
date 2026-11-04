## Why

FunkArr has five different caching mechanisms that grew organically, each with problems:
`IDistributedCache` backed by in-memory store (no enumeration for stats, JSON serialization overhead for in-process data, volatile — 30-day TTLs meaningless with Docker restarts), a custom `SearchResultCache` using `ConcurrentDictionary` that never evicts entries (memory leak), a cross-domain `IFeedCacheInvalidator` in Core so Feed can invalidate ArrApi's OutputCache (architectural boundary violation), and a dead `CacheStats` endpoint that always returns zeros because `IDistributedCache` has no enumeration API. This needs to be consolidated into a coherent, simple caching strategy.

## What Changes

- **Replace `IDistributedCache` with `IMemoryCache`** across all consumers (TvdbClient, TmdbClient, MediathekClient). No JSON serialization — direct object references. `TrackStatistics = true` enables `GetCurrentStatistics()` for metrics.
- **Replace `SearchResultCache`** (leaking ConcurrentDictionary) with `IMemoryCache.GetOrCreateAsync` in NewznabSearchService. Built-in eviction, no custom cache class needed.
- **Remove `IFeedCacheInvalidator` and OutputCache active invalidation**. Reduce NewznabFeed OutputCache TTL from 30 min to 5 min — TTL-only expiry eliminates the cross-domain coupling. Prowlarr polls every 15-60 min; 5 min cache lag is irrelevant.
- **Remove dead CacheStats code**: `QueryCacheStats`/`CacheStatsResult` messages, `/api/system/cache` endpoint, `CacheStatsResponse` model, UI "Metadata Cache" section in Settings.vue.
- **Remove `DistributedCacheExtensions`** (JSON serialization helpers no longer needed).
- **Add cache metrics via `MemoryCacheStatistics`** to Enrichment and Search telemetry — observable in Grafana instead of dead UI widgets.
- **Standardize TTLs**: TVDB 7 days (was 2-7 dynamic), TMDB 7 days (was 30), Mediathek 5 min (unchanged), Newznab search 60s (unchanged).

## Capabilities

### New Capabilities

- `memory-cache-registration`: IMemoryCache registration with SizeLimit, TrackStatistics, and shared instance configuration in CoreSetupContainer. Replaces AddDistributedMemoryCache.
- `cache-telemetry`: Cache metrics (entries gauge, hits/misses counters) via MemoryCacheStatistics on Enrichment and Search meters, registered in TelemetrySetupContainer.

### Modified Capabilities

- `tvdb-client`: Replace IDistributedCache with IMemoryCache, remove JSON serialization, fixed 7-day TTL
- `tmdb-client`: Replace IDistributedCache with IMemoryCache, remove JSON serialization, 7-day TTL (was 30)
- `mediathek-client`: Replace IDistributedCache with IMemoryCache, remove JSON serialization, 5 min TTL unchanged
- `metadata-cache`: Rewrite around IMemoryCache — no enumeration needed, no QueryCacheStats, no ClearCache
- `newznab-indexer-api`: Replace SearchResultCache with IMemoryCache in NewznabSearchService, delete SearchResultCache class

## Impact

- **FunkArr.Core**: Remove `IFeedCacheInvalidator` interface, replace `AddDistributedMemoryCache` with `AddMemoryCache` in CoreSetupContainer, delete `DistributedCacheExtensions`
- **FunkArr.Enrichment**: TvdbClient and TmdbClient switch from `IDistributedCache` to `IMemoryCache`. EnrichmentManager drops `QueryCacheStats` handler.
- **FunkArr.Search**: MediathekClient switches from `IDistributedCache` to `IMemoryCache`
- **FunkArr.ArrApi**: Delete `SearchResultCache`, `OutputCacheFeedInvalidator`. Update `NewznabSearchService` to use `IMemoryCache`. Change NewznabFeed OutputCache TTL 30min → 5min.
- **FunkArr.Feed**: FeedStoreManager drops `IFeedCacheInvalidator` dependency, `InvalidateCache()` method, `CacheInvalidated`/`CacheInvalidationFailed` message handling.
- **FunkArr.Messages**: Delete `QueryCacheStats`, `CacheStatsResult`
- **FunkArr.Api**: Delete `/api/system/cache` endpoint, `CacheStatsResponse` model
- **FunkArr.UI**: Delete "Metadata Cache" section from Settings.vue
- **Tests**: Update all affected test files (enrichment, search, ArrApi, feed)
- **Specs to retire**: `distributed-cache-extensions`, `metadata-cache-status`
