## MODIFIED Requirements

### Requirement: Metadata caching uses IMemoryCache

Metadata caching for TVDB and TMDB data SHALL use `IMemoryCache` (replacing `IDistributedCache`). Cache ownership remains in the individual clients (TvdbClient, TmdbClient), not in EnrichmentManager.

#### Scenario: Cache is in-process with no serialization

- **WHEN** metadata is cached
- **THEN** the cache SHALL store .NET object references directly via `IMemoryCache`, not serialized byte arrays

#### Scenario: Cache entries are evictable

- **WHEN** the cache reaches its size limit (2048 entries)
- **THEN** the framework SHALL evict entries based on priority and expiration (compaction)

## REMOVED Requirements

### Requirement: QueryCacheStats returns hit/miss counters

**Reason**: Cache stats are now exposed via Prometheus metrics (`cache-telemetry` capability), not via actor messages. `IMemoryCache.GetCurrentStatistics()` provides `CurrentEntryCount`, `TotalHits`, `TotalMisses` — consumed by telemetry instruments, not by UI.
**Migration**: Delete `QueryCacheStats` and `CacheStatsResult` messages. Delete `/api/system/cache` endpoint. Delete `CacheStatsResponse` API model. Delete "Metadata Cache" section from Settings.vue. Use Grafana dashboards for cache observability.

### Requirement: ClearCache removes entries by key

**Reason**: Manual cache clearing is not needed. Entries expire via TTL (7 days for enrichment data). If stale data is a concern, a container restart clears the in-memory cache. No UI or API surface for cache clearing is justified at this stage.
**Migration**: Delete any `ClearCache` message handling. If needed later, `IMemoryCache.Remove(key)` is available per-key.

### Requirement: Content-aware TTL for TVDB episodes

**Reason**: Simplified to a fixed 7-day TTL. The dynamic 2-day (active show) vs 7-day (inactive show) TTL added complexity without significant benefit — most active shows are searched frequently enough that a 7-day TTL is fine.
**Migration**: Replace dynamic TTL logic with fixed `TimeSpan.FromDays(7)` in TvdbClient.
