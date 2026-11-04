## Context

FunkArr currently uses five different caching mechanisms: `IDistributedCache` backed by in-memory store (TvdbClient, TmdbClient, MediathekClient), a custom `SearchResultCache` with `ConcurrentDictionary` (Newznab search dedup), ASP.NET OutputCache (feed responses, ruleset list, system version), a cross-domain `IFeedCacheInvalidator`, and dead CacheStats code that always returns zeros.

The distributed cache abstraction is the wrong tool — there's no distributed store, but the code pays for JSON serialization round-trips and loses the ability to enumerate entries or track statistics. The custom search cache has a memory leak. The invalidator violates domain boundaries.

## Goals / Non-Goals

**Goals:**
- Single caching abstraction (`IMemoryCache`) for all domain-level data caching
- Built-in eviction (no more memory leaks)
- Cache observability via Prometheus metrics (not dead UI widgets)
- Eliminate cross-domain coupling for cache invalidation
- Remove dead code (CacheStats endpoint, UI section)

**Non-Goals:**
- Persistent cache across container restarts (not needed — API rate limits are not a concern at current scale)
- Distributed/shared cache (single-node deployment)
- Per-domain cache isolation (data volumes are small enough for a shared instance)
- OutputCache changes beyond NewznabFeed TTL reduction (RuleSet and SystemVersion policies work fine)

## Decisions

### Decision 1: Single shared IMemoryCache instance

Use one `IMemoryCache` registered in `CoreSetupContainer` with `SizeLimit = 2048` and `TrackStatistics = true`.

**Why not separate instances?** Peak cache: ~200 TVDB + ~500 TMDB + ~50 Mediathek + ~50 Newznab = ~800 entries, well under 2048. Separate keyed instances add DI complexity for no benefit. Key prefixing (`tvdb:`, `tmdb:`, `mediathek:`, `newznab:`) prevents collisions.

**Why SizeLimit = 2048?** Generous headroom over ~800 typical entries. Size is entry count (not bytes) — each entry sets `Size = 1`. At ~10 KB average entry size, worst case is ~20 MB, which is negligible for a Docker container.

**Why TrackStatistics = true?** Enables `GetCurrentStatistics()` which returns `CurrentEntryCount`, `TotalHits`, `TotalMisses`. This feeds the new Prometheus metrics and replaces the dead CacheStats UI.

**Alternative considered:** Keyed services (`AddKeyedSingleton<IMemoryCache>`) per domain. Rejected — adds complexity, no keyed services are used in the project yet, and the data volumes don't warrant isolation.

### Decision 2: Drop IDistributedCache and DistributedCacheExtensions

Replace `IDistributedCache` with `IMemoryCache` in TvdbClient, TmdbClient, MediathekClient constructors. Delete `DistributedCacheExtensions` (the `GetAsync<T>`/`SetAsync<T>` JSON helpers).

**Why?** `IDistributedCache` forces serialization (`byte[]`), has no enumeration/statistics API, and the in-memory backend (`AddDistributedMemoryCache`) is just a `MemoryCache` wrapper with extra overhead. `IMemoryCache` stores object references directly — zero serialization cost.

**Migration pattern per client:**
```
// Before (IDistributedCache)
var cached = await _cache.GetAsync<TvdbEpisode[]>(key, ct);
if (cached is not null) return cached;
var result = await FetchFromApi(...);
await _cache.SetAsync(key, result, ttl, ct);

// After (IMemoryCache)
if (_cache.TryGetValue(key, out TvdbEpisode[]? cached)) return cached;
var result = await FetchFromApi(...);
_cache.Set(key, result, new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
    Size = 1
});
```

### Decision 3: Replace SearchResultCache with IMemoryCache

Delete the custom `SearchResultCache` class. Use `IMemoryCache` directly in `NewznabSearchService` with `GetOrCreateAsync` for atomic cache-or-fetch.

**Why?** The current `ConcurrentDictionary` never evicts entries — only checks TTL on read. Over days/weeks the dictionary grows unbounded. `IMemoryCache` has built-in eviction via `SizeLimit` and `CompactionPercentage`.

**Deduplication:** The `Lazy<Task>` pending-request dedup in `SearchResultCache` prevents thundering herd. `GetOrCreateAsync` provides similar semantics — if two calls arrive for the same key simultaneously, only one factory executes. However, the exact behavior depends on the `IMemoryCache` implementation. If stronger dedup is needed, a thin `SemaphoreSlim`-per-key wrapper can be added, but this is unlikely to matter at FunkArr's request volume (Sonarr/Radarr, not public traffic).

### Decision 4: TTL-only OutputCache, no active invalidation

Reduce NewznabFeed OutputCache TTL from 30 minutes to 5 minutes. Remove the entire `IFeedCacheInvalidator` chain.

**Why 5 minutes?** Prowlarr's RSS sync interval is configurable (default 15-60 min). A 5-minute cache means at most one extra Prowlarr poll sees stale data after a feed update. This is acceptable — Prowlarr's own polling interval is the real latency bottleneck, not the server cache.

**What gets deleted:**
- `IFeedCacheInvalidator` interface (Core)
- `OutputCacheFeedInvalidator` implementation (ArrApi)
- `FeedStoreManager.InvalidateCache()` method and the PipeTo chain
- `CacheInvalidated` / `CacheInvalidationFailed` private message records
- `IFeedCacheInvalidator` constructor parameter on FeedStoreManager

**Alternative considered:** Move the interface out of Core into a shared "infrastructure contracts" project. Rejected — the 5-min TTL makes active invalidation unnecessary, so the whole mechanism is deleted rather than relocated.

### Decision 5: Remove CacheStats dead code

Delete the entire QueryCacheStats/CacheStatsResult flow: messages, EnrichmentManager handler, `/api/system/cache` endpoint, `CacheStatsResponse` model, Settings.vue "Metadata Cache" section.

**Why?** The endpoint has always returned zeros. `IDistributedCache` has no enumeration API — the handler was stubbed and never implemented. With `IMemoryCache.GetCurrentStatistics()`, real stats flow through Prometheus metrics instead.

### Decision 6: Standardized TTLs

| Cache | TTL | Rationale |
|-------|-----|-----------|
| TVDB episodes | 7 days fixed | Simplification — dynamic 2d/7d added complexity without meaningful benefit |
| TMDB movies | 7 days | Reduced from 30d — movie metadata rarely changes, but 30d was pointless with Docker restarts |
| Mediathek queries | 5 minutes | Unchanged — short TTL matches API query freshness needs |
| Newznab search | 60 seconds | Unchanged — matches ArrApiOptions.SearchCacheTtlSeconds default |
| OutputCache NewznabFeed | 5 minutes | Reduced from 30min — enables removal of active invalidation |

## Risks / Trade-offs

- **[Cache lost on restart]** → Accepted. In-memory cache is volatile. First requests after restart hit external APIs. TVDB/TMDB rate limits are generous enough for a single user's library. If this becomes a problem, `IMemoryCache` can be swapped for a persistent store later without changing consumer code patterns.
- **[Shared cache eviction]** → Mitigated by generous SizeLimit (2048) vs typical usage (~800). Short-TTL entries (Mediathek 5min, Newznab 60s) self-evict before hitting limits. If TVDB+TMDB entries grow beyond expectations, compaction evicts least-recently-used entries — acceptable since a cache miss just means one extra API call.
- **[GetOrCreateAsync dedup weaker than Lazy\<Task\>]** → Accepted. The `Lazy<Task>` pattern in `SearchResultCache` guaranteed exactly-once execution for concurrent identical requests. `GetOrCreateAsync` may occasionally execute the factory twice under race conditions. At FunkArr's request volume (single Sonarr/Radarr instance), this is negligible.
- **[5-min feed cache lag]** → Accepted. Prowlarr polls every 15-60 min. A 5-minute server-side cache adds at most one stale poll cycle. Users searching manually in Prowlarr might see slightly stale data, but this is the same experience they'd have with Prowlarr's own caching.

## Open Questions

None — all decisions were made during the exploration phase.
