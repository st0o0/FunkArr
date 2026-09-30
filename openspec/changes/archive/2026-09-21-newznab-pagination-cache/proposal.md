# Newznab Pagination Cache

## Problem

When Sonarr/Prowlarr search for a series via the Newznab API, every paginated request (offset=0, offset=100, offset=200...) creates a brand-new `TvSearchWorker` actor that performs a full pipeline: Mediathek query → scoring → enrichment → history recording. A single Tatort episode search generated **40 identical history entries** (20 pages × 2 clients) within 40 seconds.

Two bugs compound the issue:

1. **`ToRss` ignores the offset parameter** — it does `.Take(limit)` instead of `.Skip(offset).Take(limit)`, so every page returns the same first 100 items. Sonarr never reaches the end and paginates until its internal limit.

2. **No result caching** — each paginated request triggers a complete search pipeline (Mediathek HTTP call, scoring, TVDB enrichment, history write) even though the result set is identical.

## Scope

Add a short-lived result cache at the Newznab `SearchHandler` level. The first request for a given logical search (same query/tvdbId/season/episode) executes the full pipeline and caches the result. Subsequent paginated requests serve slices from the cache without touching the actor system.

### In scope

- `SearchResultCache` service: thread-safe, TTL-based, keyed by logical search parameters (excluding offset/limit)
- Concurrent request coalescing: if two clients request the same search simultaneously, only one actor call is made
- Fix `ToRss` pagination: `.Skip(offset).Take(limit)` instead of `.Take(limit)`
- Wire cache into `SearchHandler` via constructor injection

### Out of scope

- Changes to SearchManager, TvSearchWorker, or HistoryWorker actors
- Mediathek-level caching (separate concern)
- Deduplication of existing history entries

## Approach

### Cache design

```
SearchResultCache (Singleton)
  ConcurrentDictionary<string, CacheEntry>
  CacheEntry = { Items: SearchResultItem[], Expiry: DateTimeOffset }
  
  + Lazy<Task> coalescing for concurrent identical requests
  + TTL: 60 seconds (configurable)
  + Memory: ~150 KB per entry (300 items × 500 bytes)
```

### Cache key

Derived from the `SearchCommand`, excluding offset and limit:

- TV search: `tv:{query}:{tvdbId}:{imdbId}:{season}:{episode}`
- Movie search: `movie:{query}:{imdbId}:{tmdbId}`
- General search: `general:{query}:{cat}`

Source (sonarr/prowlarr) is excluded — the result set is identical regardless of caller.

### Flow

```
SearchHandler.Handle(req):
  key = BuildCacheKey(cmd)
  
  Cache HIT:
    items = cache[key]
    paged = items.Skip(offset).Take(limit)
    total = items.Length
    → XML response (no actor call)

  Cache MISS:
    cmd = cmd with { Offset = null }  // always fetch full set
    response = await Ask SearchManager
    cache.Set(key, response.Items, TTL)
    paged = items.Skip(offset).Take(limit)
    total = items.Length
    → XML response
```

### Impact

| Metric | Before | After |
|--------|--------|-------|
| Mediathek HTTP calls per search | 40 | 1 |
| Scoring runs | 40 | 1 |
| Enrichment calls | 40 | 1 |
| History entries | 40 | 1 |
| Sonarr gets correct pages | No (same 100 items every page) | Yes |

## Files changed

| File | Change |
|------|--------|
| `FunkArr.ArrApi/Newznab/SearchResultCache.cs` | **New** — cache service |
| `FunkArr.ArrApi/Newznab/SearchHandler.cs` | Add cache parameter, pagination fix, cache lookup/store |
| `FunkArr.ArrApi/Newznab/NewznabApiEndpoints.cs` | Resolve and pass cache to SearchHandler |
| `FunkArr.ArrApi/ServiceCollectionExtensions.cs` | Register SearchResultCache singleton |
| `FunkArr.ArrApi.Tests/SearchResultCacheTests.cs` | **New** — cache unit tests |
| `FunkArr.ArrApi.Tests/SearchHandlerTests.cs` | **New** or extended — pagination and caching tests |
