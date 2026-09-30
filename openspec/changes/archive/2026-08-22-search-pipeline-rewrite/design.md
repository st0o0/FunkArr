## Context

The current search pipeline uses three monolithic child actors (`TvSearchActor`, `MovieSearchActor`, `TextSearchActor`) that each perform the entire search workflow inline: HTTP calls, matching, quality probing, and scoring. The parent `SearchActor` uses `ReceiveAsync` for TV searches (to await TVDB resolution and rules), which suspends the mailbox and prevents request coalescing.

The architecture-redesign.md (§3) defines the target: a `SearchCoordinator` singleton with five specialized child workers, orchestrated via `Receive` + `PipeTo`. This design document covers the implementation approach.

## Goals / Non-Goals

**Goals:**
- Replace `SearchActor` + 3 children with `SearchCoordinator` + 5 workers
- Enable topic-level request coalescing for season refresh scenarios
- Keep mailbox open during pipeline execution via `Receive` + `PipeTo`
- Wrap `MediathekClient`, `TvdbClient`/`TmdbClient`, `QualityProbeService` in dedicated actors
- Add persistent caches for show resolution and quality probing (Tier 2)

**Non-Goals:**
- SearchRequestCoordinator shard entity (deferred — requires Cluster.Sharding infrastructure not yet in place; will be added when download sharding lands in Phase 2)
- MatchQualityWorker event-sourcing (Phase 3: ruleset-extensions)
- Akka.Streams throttled channels inside workers (initial implementation uses simple async; Streams throttling is an optimization added later)

## Decisions

### D1: Coalescing key granularity

**Decision:** Coalesce at topic level, not episode level.

TV coalescing key: `"tv:{tvdbId}:{searchTerm}"`. Movie: `"movie:{imdbId}"`. Text: `"text:{query}"`.

Episode-level filtering happens after the coalesced fetch returns — the coordinator applies per-episode filters to the shared result set before replying to each caller.

**Why:** Sonarr season refresh sends 12 episode requests for the same show. All 12 hit the same Mediathek results. Coalescing at episode level would still make 12 HTTP calls. Topic-level coalescing makes one call and filters 12 ways.

**Alternative considered:** Per-episode coalescing — rejected because the Mediathek API returns topic-level results, not episode-level.

### D2: PipeTo pipeline state tracking

**Decision:** A `PipelineState` record per in-flight coalesce key tracks intermediate results and pending callers.

```
PipelineState:
  CoalesceKey: string
  Callers: List<(IActorRef, SearchParams)>   ← all pending callers
  ShowInfo: ShowInfo?                         ← from ShowResolverWorker
  Rules: IReadOnlyList<Rule>?                ← from RuleSetCoordinator
  RawItems: MediathekResultItem[]?           ← from MediathekGatewayWorker
  MatchResult: (IReadOnlyList<SearchResult>, MatchRecord)?  ← from MatchWorker
  ProbedResults: IReadOnlyList<SearchResult>? ← from QualityProbeWorker
```

The coordinator maintains `Dictionary<string, PipelineState> _inflight`. Each PipeTo callback advances the state for one key and triggers the next step.

**Why:** PipeTo callbacks are message handlers — they run on the actor's thread, one at a time. The dictionary provides the "continuation" that ReceiveAsync would normally hold on the stack.

### D3: Worker actor types — permanent vs transient

**Decision:** All five workers are permanent children, created in `PreStart`.

- ShowResolverWorker — permanent, owns show cache
- MediathekGatewayWorker — permanent, stateless but rate-limited
- MatchWorker — permanent, stateless
- QualityProbeWorker — permanent, owns probe cache
- ScoreWorker — permanent, stateless

**Why:** Even stateless workers earn their place: pipeline uniformity (every step is Ask), supervision isolation (regex crash in MatchWorker doesn't kill the coordinator), and mailbox isolation (5000 items × 20 rules is CPU work that shouldn't block coordinator message processing).

### D4: Cache key change from episode-level to topic-level

**Decision:** The result cache key changes from `"tv:{tvdbId}:{season}:{episode}:{searchTerm}"` to `"tv:{tvdbId}:{searchTerm}"` for the topic-level cache. Per-episode filtering is applied on cache hit.

**Why:** With coalescing, the cache stores topic-level results. A cache hit for the same topic with a different episode still returns valid results — the coordinator just filters differently.

**Migration:** The cache is in-memory, so no data migration. After restart, the cache rebuilds naturally.

### D5: ShowResolverWorker and QualityProbeWorker persistence

**Decision:** Both workers are event-sourced (Tier 2) with snapshots every 500 events.

ShowResolverWorker events: `ShowResolved(tvdbId, showInfo)`, `MovieResolved(imdbId, movieInfo)`.
QualityProbeWorker events: `UrlProbed(url, qualityInfo)`.

**Why:** These caches are expensive to rebuild (HTTP calls to TVDB/TMDB, HEAD requests to CDNs). Event-sourcing provides warm restart and full audit trail. Snapshots keep recovery fast.

**Alternative considered:** Snapshot-only persistence — rejected because event-sourcing lets us rebuild with different eviction rules without losing data.

### D6: Existing services become actor internals

**Decision:** `MediathekClient`, `TvdbClient`, `TmdbClient`, `QualityProbeService` remain as service classes but are consumed exclusively by their owning worker actors. No other actor references them directly.

**Why:** The service classes contain the HTTP logic. The actor wrapper adds: mailbox isolation, supervision, caching, and rate limiting. Rewriting the HTTP logic would be churn.

### D7: External API surface unchanged

**Decision:** The public message types (`SearchActor.TvSearchRequest`, `SearchActor.MovieSearchRequest`, `SearchActor.TextSearchRequest`, `SearchActor.SearchResponse`) remain unchanged but move to `SearchCoordinator`. Controllers use the same `Ask` pattern.

**Why:** Zero-disruption for controllers and tests. The rename from `SearchActor` to `SearchCoordinator` is reflected in the registration and resolution, but message types stay the same.

## Risks / Trade-offs

**[Risk] PipeTo complexity** — PipeTo chains are harder to read and debug than linear async code.
→ Mitigation: Each pipeline step is a separate private method (`OnShowResolved`, `OnItemsFetched`, etc.) with clear naming. PipelineState makes the continuation explicit.

**[Risk] Coalescing correctness** — Multiple callers for the same key receive the same results, but each needs different episode filtering.
→ Mitigation: The `Callers` list stores each caller's original `SearchParams`. Finalize step applies per-caller filtering before reply.

**[Risk] Persistence journal growth** — ShowResolverWorker and QualityProbeWorker accumulate events over time.
→ Mitigation: Snapshots every 500 events. TTL enforcement during recovery (skip events older than cache TTL). Future: explicit eviction events like MatchQualityWorker.

**[Trade-off] Five workers for simple operations** — MatchWorker and ScoreWorker are thin wrappers around pure functions.
→ Accepted: Pipeline uniformity and supervision isolation outweigh the minor overhead. Adding external scoring later changes nothing in the coordinator.

## Migration Plan

1. Create new SearchCoordinator + 5 workers in `FunkArr.Search/`
2. Update `FunkArrActorSystemSetup` to register `SearchCoordinator` instead of `SearchActor`
3. Update controllers to resolve `SearchCoordinator` instead of `SearchActor`
4. Delete `SearchActor`, `TvSearchActor`, `MovieSearchActor`, `TextSearchActor`
5. Update tests to target new actor topology

No data migration needed — search caches are in-memory. Persistence for ShowResolver and QualityProbe is new (not migrating existing data).

Rollback: revert the commit. No persistent state to clean up.
