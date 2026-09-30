## Why

The current search pipeline bundles HTTP calls, matching, quality probing, and scoring into monolithic child actors (`TvSearchActor`, `MovieSearchActor`, `TextSearchActor`). Each actor uses `ReceiveAsync`, which suspends the mailbox during `await` — preventing request coalescing when Sonarr sends 12 episode searches for the same show in a season refresh. The redesign splits the pipeline into specialized workers orchestrated by a `SearchCoordinator` using `Receive` + `PipeTo`, enabling topic-level coalescing, per-step supervision, and isolation of CPU-heavy matching from I/O-bound HTTP calls.

## What Changes

- Replace `SearchActor` with `SearchCoordinator` (Singleton) that owns cache, coalescing, and pipeline orchestration via `Receive` + `PipeTo`
- Replace `TvSearchActor`, `MovieSearchActor`, `TextSearchActor` with five specialized child workers:
  - `ShowResolverWorker` — TVDB/TMDB resolution with persistent cache (Tier 2, event-sourced + snapshot)
  - `MediathekGatewayWorker` — throttled MediathekViewWeb access via Akka.Streams
  - `MatchWorker` — delegates to `RuleSetMatchingEngine` / `MatchingPipeline` (stateless)
  - `QualityProbeWorker` — URL probing with persistent cache (Tier 2, event-sourced + snapshot)
  - `ScoreWorker` — ranking via pure scoring function (stateless)
- Add `SearchRequestCoordinator` (ShardRegion, no persistence) for per-request audit trail with deterministic entity ID from query params
- Introduce topic-level request coalescing: `"tv:{tvdbId}:{searchTerm}"` for TV, `"movie:{imdbId}"` for movies
- Move from `ReceiveAsync` to `Receive` + `PipeTo` in the coordinator for mailbox availability during pipeline execution

## Capabilities

### New Capabilities
- `search-coordinator`: SearchCoordinator singleton with cache, coalescing, PipeTo pipeline orchestration, and five specialized child workers
- `show-resolver-worker`: TVDB/TMDB resolution worker with Tier 2 event-sourced persistence and Akka.Streams throttled HTTP channel
- `mediathek-gateway-worker`: Throttled MediathekViewWeb access worker with Akka.Streams rate limiting
- `search-request-tracking`: SearchRequestCoordinator shard entity for per-request audit trail (no persistence, passivated after inactivity)

### Modified Capabilities
- `search-routing`: Newznab/SABnzbd controllers route through SearchRequestCoordinator shard instead of directly to SearchActor
- `quality-probing`: QualityProbeWorker becomes a permanent actor child of SearchCoordinator with Tier 2 event-sourced cache instead of a stateless service
- `match-ledger`: MatchQualityWorker moves under RuleSetCoordinator (deferred to Phase 3 `ruleset-extensions`)

## Impact

- **Actors**: `SearchActor`, `TvSearchActor`, `MovieSearchActor`, `TextSearchActor` replaced by `SearchCoordinator` + 5 workers + `SearchRequestCoordinator` shard
- **Services**: `MediathekClient`, `TvdbClient`, `TmdbClient`, `QualityProbeService` move from DI-injected into actors to wrapped inside dedicated workers
- **Registration**: `FunkArrActorSystemSetup` changes from `WithResolvableActors<SearchActor>` to Singleton + ShardRegion registration
- **Persistence**: Two new event-sourced actors (ShowResolverWorker, QualityProbeWorker) with Tier 2 persistence — new journal entries, no migration needed
- **Tests**: `TvSearchActorTests`, `MovieSearchActorTests`, `TextSearchActorTests` replaced by coordinator + per-worker tests
- **Dependencies**: Akka.Cluster.Sharding required for SearchRequestCoordinator (single-node sharding)
