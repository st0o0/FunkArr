## Context

MetadataResolverManager is a Cluster Singleton that mediates between callers (search/match actors) and external metadata providers (TVDB, TMDB). It maintains an in-memory cache and delegates API fetches to router pools of TvdbResolverActor/TmdbResolverActor.

Currently the cache stores resolved results (ResolvedEpisode[], MovieResolved[]) keyed by provider ID. This is incorrect because resolution depends on request-specific parameters (Season, Config, Candidates) that are not part of the key. The pool actors both fetch and resolve, then send back a cache update with the resolved results.

## Goals / Non-Goals

**Goals:**
- Correct cache semantics: cache key fully identifies cached value (raw API data keyed by provider ID)
- Re-resolve on every request using current parameters - always correct results
- Clear separation: pool actors handle I/O, Manager handles caching + resolution
- Bounded memory: periodic eviction of expired entries

**Non-Goals:**
- Changing the external message contract (ResolveEpisodes, ResolveMovie, EpisodesResolved, MoviesResolved stay the same)
- Introducing a distributed or persistent cache
- Changing TTL values or TTL determination logic
- Modifying EpisodeResolver or MovieResolver logic

## Decisions

### Decision 1: Cache raw API responses, not resolved results

Cache TvdbEpisode[] keyed by TvdbId and (TmdbMovie, string[] altTitles) keyed by TmdbId. Resolution is pure CPU (array matching, Levenshtein) and takes microseconds. The API calls take hundreds of milliseconds and are rate-limited. Caching the cheap-to-compute result while the expensive-to-fetch input is discarded was backwards.

Alternative considered: composite cache key including Season + Config + Candidates hash. Rejected because it would multiply cache entries per series and still miss on any parameter change. Caching raw data is simpler and always correct.

### Decision 2: Pool actors become pure fetch actors

TvdbResolverActor returns a TvdbFetchResult(TvdbId, TvdbEpisode[]) to the Manager. TmdbResolverActor returns a TmdbFetchResult(TmdbId, TmdbMovie, string[] AltTitles) to the Manager. They no longer receive Candidates or Config. They no longer resolve.

The Manager receives the fetch result, caches the raw data, then resolves using the original request's parameters and responds to the original caller.

This requires the Manager to remember the original Sender and request parameters while the fetch is in-flight. Use a dictionary of pending requests keyed by (provider, id) to stash the original Sender and request parameters.

### Decision 3: Pending request tracking

When a cache miss occurs, the Manager must:
1. Remember who asked (Sender) and with what parameters
2. Forward a fetch-only message to the pool
3. On fetch result: cache raw data, resolve, respond to original Sender

Multiple callers may request the same ID concurrently. Use a `Dictionary<int, List<PendingRequest>>` per provider. When a fetch result arrives, resolve for each pending request individually (each may have different Season/Config/Candidates) and respond to each original Sender.

If a fetch is already in-flight for the same ID, don't send a second fetch request - just add to the pending list.

### Decision 4: Scheduled eviction timer

Use `Context.System.Scheduler.ScheduleTellRepeatedly` to send an `EvictExpired` message to Self every 30 minutes. On receiving it, iterate both caches and remove entries where `IsExpired` is true.

This bounds memory growth. The 30-minute interval is coarse enough to avoid overhead but fine enough to prevent significant accumulation.

### Decision 5: Manager resolves using injected resolvers

The Manager needs EpisodeResolver and MovieResolver injected via constructor (DI). Currently only pool actors have these. Move the dependency to the Manager. Pool actors keep their API clients but drop the resolvers.

## Risks / Trade-offs

- [Pending request map grows if API is slow/down] - Mitigated: fetch actors already respond with failure messages. On failure, clear pending requests for that ID and forward the error to all waiting Senders.
- [Re-resolving on every hit costs CPU] - Negligible: resolution is array iteration with string comparison. Sub-millisecond for typical episode counts (< 500 per series).
- [Breaking change to internal messages] - Acceptable: InternalMessages are internal to the domain, not part of any external contract. Version 0.x allows clean breaks.
