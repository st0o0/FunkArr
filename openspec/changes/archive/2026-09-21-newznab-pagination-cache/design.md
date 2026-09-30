# Design: Newznab Pagination Cache

## Architecture

The cache sits at the Newznab HTTP boundary, between the incoming Sonarr/Prowlarr request and the actor system. It is invisible to the actor system — SearchManager, TvSearchWorker, and HistoryWorker remain unchanged.

```
  Sonarr/Prowlarr
       │
       ▼
  NewznabApiEndpoints
       │
       ▼
  SearchHandler ──── SearchResultCache (singleton)
       │                    │
       │  cache miss        │ cache hit
       │                    │
       ▼                    │
  SearchManager             │
       │                    │
       ▼                    │
  TvSearchWorker            │
       │                    │
       ▼                    │
  SearchResultItem[]  ──────┘
       │
       ▼
  .Skip(offset).Take(limit)
       │
       ▼
  Newznab XML Response
```

## SearchResultCache

Thread-safe singleton service. Stores completed search results keyed by logical search parameters.

### Interface

```csharp
internal sealed class SearchResultCache
{
    // Try to get a cached result. Returns false if not cached or expired.
    bool TryGet(string key, out SearchResultItem[] items);
    
    // Store a result with TTL.
    void Set(string key, SearchResultItem[] items);
    
    // Get or compute: if another caller is already computing the same key,
    // wait for their result instead of starting a parallel search.
    Task<SearchResultItem[]> GetOrAddAsync(
        string key, Func<Task<SearchResultItem[]>> factory);
    
    // Build a cache key from a SearchCommand (offset/limit excluded).
    static string BuildKey(SearchCommand cmd);
}
```

### Key generation

```
TV search:      "tv:{query}:{tvdbId}:{imdbId}:{season}:{episode}"
Movie search:   "movie:{query}:{imdbId}:{tmdbId}"
General search: "general:{query}:{cat}"
```

- `query` is lowercased and trimmed
- Null values rendered as empty string
- Source (sonarr/prowlarr) excluded — same search = same results

### Concurrency: GetOrAddAsync

When Prowlarr and Sonarr simultaneously request the same search:

```
Thread A (Prowlarr, offset=0): GetOrAddAsync("tv::83214::2026:18", search)
  → cache miss, starts search, holds Lazy<Task>
  
Thread B (Sonarr, offset=0):   GetOrAddAsync("tv::83214::2026:18", search)  
  → sees pending Lazy<Task>, awaits same task
  → no second actor call

Thread A completes → both get result → cache populated
Thread C (offset=100): GetOrAddAsync → cache hit → no actor call
```

Implementation uses `ConcurrentDictionary<string, Lazy<Task<SearchResultItem[]>>>` for in-flight dedup, separate from the TTL cache.

### TTL and eviction

- Default TTL: 60 seconds (configurable via `NewznabOptions.CacheTtlSeconds`)
- Eviction: lazy on access (check expiry on TryGet) + periodic sweep every 5 minutes
- Max entries: no hard limit (natural bound: number of distinct searches within TTL window)

## SearchHandler changes

### Before

```csharp
private async Task<IResult> AskAndFormat(SearchCommand cmd, int offset, int limit, ...)
{
    var response = await gateway.Ask<SearchCommandResponse>(cmd, _searchTimeout);
    // ... ToRss(completed, offset, limit, category)
}
```

### After

```csharp
private async Task<IResult> HandleCached(SearchCommand cmd, int offset, int limit, ...)
{
    var key = SearchResultCache.BuildKey(cmd);
    var items = await cache.GetOrAddAsync(key, async () =>
    {
        var fullCmd = cmd with { Offset = null, Limit = null };
        var response = await gateway.Ask<SearchCommandResponse>(fullCmd, _searchTimeout);
        return response is SearchCommandCompleted completed
            ? completed.Items
            : [];
    });
    
    var paged = items.Skip(offset).Take(limit).ToArray();
    var total = items.Length;
    // ... build XML with correct total and paged items
}
```

Key decisions:
- `Offset = null, Limit = null` sent to SearchManager — the actor fetches everything once
- The cache stores the full sorted result set
- Pagination is pure slicing on cached data

### ToRss fix

```csharp
// Before:
var paged = completed.Items.Take(limit);

// After:
var paged = completed.Items.Skip(offset).Take(limit);
```

This fix applies regardless of caching — even without the cache, the pagination must be correct.

## SearchCommand changes

`SearchCommand` is a record, so `with { Offset = null, Limit = null }` works out of the box. No message changes needed.

When the actor receives `Offset = null`, the TvSearchWorkerState.TryGetMediathekQuery already defaults to `Offset ?? 0` and `Limit ?? 50`. We may want to increase the null-limit default or pass a higher value explicitly so the full result set is fetched. This is a tuning decision — the current 50-item default may be too low for the "fetch all" case.

## Decision: Limit for cached searches

When the cache triggers a full search (no offset/limit), how many items should the actor fetch from the Mediathek?

Option A: Keep current default (50) — small result set, fast, but Sonarr may miss items
Option B: Use a higher fixed limit (e.g., 500) — captures more variants
Option C: Use MediathekViewWeb's max (10000) — exhaustive but expensive

**Decision: Option B (500)**. This matches `NewznabApiEndpoints.MaxLimit` and gives enough headroom for variant expansion while keeping the Mediathek call reasonable. After scoring + expansion, typical result sets are 100-500 variants.

## Test plan

### SearchResultCache tests

| Test | Description |
|------|-------------|
| `TryGet_UnknownKey_ReturnsFalse` | Cache miss on empty cache |
| `Set_ThenTryGet_ReturnsCachedItems` | Basic set/get |
| `TryGet_AfterTtlExpires_ReturnsFalse` | TTL eviction |
| `BuildKey_SameParams_SameKey` | Key stability |
| `BuildKey_DifferentOffset_SameKey` | Offset excluded from key |
| `BuildKey_DifferentTvdbId_DifferentKey` | Key varies on search params |
| `BuildKey_TvVsMovie_DifferentKey` | Search type separation |
| `GetOrAddAsync_ConcurrentSameKey_SingleFactory` | Only one factory invocation |
| `GetOrAddAsync_ConcurrentDifferentKeys_ParallelFactories` | Independent searches run in parallel |
| `GetOrAddAsync_FactoryFails_DoesNotCache` | Failed searches not cached |

### SearchHandler tests

| Test | Description |
|------|-------------|
| `Handle_CacheMiss_CallsSearchManager` | First request triggers actor search |
| `Handle_CacheHit_SkipsSearchManager` | Subsequent request served from cache |
| `Handle_Offset0_ReturnsFirstPage` | Correct first page |
| `Handle_Offset100_ReturnsSecondPage` | Correct second page |
| `Handle_OffsetBeyondTotal_ReturnsEmpty` | Pagination terminates |
| `Handle_TotalReflectsFullResultSet` | XML total = cached items count |

### ToRss tests

| Test | Description |
|------|-------------|
| `ToRss_Offset0_ReturnsFirstItems` | Skip(0).Take(limit) |
| `ToRss_Offset100_SkipsFirstItems` | Skip(100).Take(limit) |
| `ToRss_OffsetBeyondItems_ReturnsEmpty` | Graceful end |
| `ToRss_TotalIsFullCount_NotPageCount` | Total = all items |
