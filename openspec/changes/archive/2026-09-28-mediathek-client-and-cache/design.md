## Context

The MediathekViewWebManager actor currently owns HTTP requests, caching (custom `Dictionary<string, CacheEntry>` with 5min TTL and timer-based cleanup), backpressure (stash-based, max 3 in-flight), query building, and response mapping. The Enrichment domain already separates these concerns: TmdbClient and TvdbClient are plain DI services with typed HttpClient and IMemoryCache; actors above them handle orchestration.

Three different caching mechanisms exist across the codebase:
- MediathekViewWebManager: custom Dictionary with manual expiry + cleanup timer
- TmdbClient: IMemoryCache with 30-day TTL
- TvdbClient: IMemoryCache with adaptive TTL (2 or 7 days)

## Goals / Non-Goals

**Goals:**
- Extract a `MediathekClient` typed DI service following the TmdbClient/TvdbClient pattern
- Slim MediathekViewWebManager to backpressure/concurrency only
- Unify all three clients on `IDistributedCache` with typed extension methods
- Maintain identical runtime behavior (AddDistributedMemoryCache is in-process)

**Non-Goals:**
- Adding Redis/Valkey infrastructure (future change)
- Changing cache TTLs or eviction strategies
- Modifying the MediathekViewWeb query API or response format
- Changing the enrichment actor topology

## Decisions

### Decision 1: Typed cache extension methods in FunkArr.Core

Create `DistributedCacheExtensions` in FunkArr.Core with `GetAsync<T>` and `SetAsync<T>` using System.Text.Json. All three clients use these instead of raw byte[] operations.

**Why Core:** Core already bundles shared framework references. All domain projects reference Core, so extensions are available everywhere without a new project.

**Alternative considered:** A separate `FunkArr.Caching` project. Rejected - overkill for two extension methods with no domain logic.

### Decision 2: MediathekClient receives a built query object, not raw parameters

The actor keeps the MediathekQueryBuilder and passes a built query object (the serializable query record) to the client. The client handles JSON serialization, HTTP POST, response deserialization, and caching.

**Why:** The query builder is tightly coupled to the actor's message-to-query mapping logic. Moving it into the client would pull actor concerns into a DI service.

**Alternative considered:** Client owns query building end-to-end. Rejected - the actor already translates domain messages into query parameters, and the query builder is part of that translation.

### Decision 3: Cache key strategy

- MediathekClient: Hash of the serialized JSON query body (same as current, but using the query object's deterministic serialization)
- TmdbClient: `tmdb:movie:{id}` (unchanged)
- TvdbClient: `tvdb:episodes:{id}` (unchanged)

All keys are strings. IDistributedCache uses string keys natively.

### Decision 4: Drop cache entry count telemetry gauge

The current `(cache as MemoryCache)?.Count` pattern does not work with IDistributedCache. Rather than adding Interlocked counters (which cannot track evictions), drop the cache entry count gauges. Cache hit/miss counters already provide better operational insight.

**Alternative considered:** Interlocked increment on Set, but no eviction callback means the count drifts. Not worth the complexity for a gauge that rarely drives operational decisions.

### Decision 5: AddDistributedMemoryCache() registration in Core setup

Register `services.AddDistributedMemoryCache()` once in `ServiceSetupContainer` (Core). Both Search and Enrichment domains get IDistributedCache from the same registration.

**Alternative considered:** Each domain registers its own cache. Rejected - leads to duplicate registrations and potential conflicts.

### Decision 6: Actor removes IWithTimers

MediathekViewWebManager currently implements `IWithTimers` solely for periodic cache cleanup. With caching moved to IDistributedCache (which handles its own expiry), the timer is no longer needed. The actor drops `IWithTimers`.

## Risks / Trade-offs

- **Serialization overhead**: IDistributedCache serializes to byte[] on every Get/Set vs IMemoryCache storing object references directly. For the in-process backend this is negligible (< 1ms per operation). If Redis is added later, the serialization cost is inherent.
  Mitigation: Monitor cache hit/miss latency via existing telemetry.

- **Cache count telemetry loss**: Dropping the gauge removes one signal.
  Mitigation: Hit/miss counters are the better operational metric. If count is needed later, a Redis INFO command can provide it.

- **Breaking change to metadata-cache spec**: The spec currently says "in-memory cache" and "EnrichmentManager SHALL maintain". The cache moves from actor-owned IMemoryCache to injected IDistributedCache in each client.
  Mitigation: The spec describes caching requirements (TTLs, keys, invalidation), not implementation. The delta spec updates the mechanism while preserving all behavioral requirements.
