## Purpose

SearchCoordinator is a singleton actor that serves as the sole entry point for all search requests. It orchestrates a pipeline of five specialized child workers (ShowResolverWorker, MediathekGatewayWorker, MatchWorker, QualityProbeWorker, ScoreWorker) using Receive + PipeTo, with topic-level caching and request coalescing.

## Requirements

### Requirement: SearchCoordinator singleton registration
The system SHALL register a `SearchCoordinator` actor as a Singleton in the ActorSystem, resolvable via `IActorRegistry`. The coordinator SHALL be the sole entry point for all search requests from controllers.

#### Scenario: Registration and resolution
- **WHEN** the application starts
- **THEN** `SearchCoordinator` SHALL be registered and resolvable via `Context.GetActorAsync<SearchCoordinator>()`

#### Scenario: Controllers ask SearchCoordinator
- **WHEN** a Newznab controller receives a search request
- **THEN** it SHALL ask `SearchCoordinator` using the existing message types (`TvSearchRequest`, `MovieSearchRequest`, `TextSearchRequest`) and receive a `SearchResponse`

### Requirement: Topic-level result cache
The `SearchCoordinator` SHALL maintain an in-memory result cache keyed at topic level with a 55-minute TTL. Cache keys SHALL be: `"tv:{tvdbId}:{searchTerm}"` for TV, `"movie:{imdbId}"` for movies, `"text:{query}"` for text searches.

#### Scenario: Cache hit returns results immediately
- **WHEN** `SearchCoordinator` receives a `TvSearchRequest` for tvdbId=370761 and a non-expired cache entry exists for key `"tv:370761:Tatort"`
- **THEN** it SHALL reply with the cached results without invoking any worker

#### Scenario: Cache miss triggers pipeline
- **WHEN** `SearchCoordinator` receives a `TvSearchRequest` for tvdbId=370761 and no cache entry exists
- **THEN** it SHALL start the worker pipeline and cache the results on completion

#### Scenario: Per-episode filtering on cache hit
- **WHEN** a TV cache hit returns topic-level results and the request specifies season=1, episode=5
- **THEN** `SearchCoordinator` SHALL filter the cached results to match the requested episode before replying

### Requirement: Topic-level request coalescing
The `SearchCoordinator` SHALL coalesce concurrent requests for the same topic into a single worker pipeline invocation. Additional requests arriving while a pipeline is in-flight for the same coalesce key SHALL be appended to a pending callers list and replied to when the pipeline completes.

#### Scenario: Two TV requests for same show coalesced
- **WHEN** `SearchCoordinator` receives `TvSearchRequest(tvdbId=370761, season=1, episode=3)` followed by `TvSearchRequest(tvdbId=370761, season=1, episode=4)` before the first completes
- **THEN** only one worker pipeline SHALL execute, and both callers SHALL receive their episode-filtered results when it completes

#### Scenario: Different shows not coalesced
- **WHEN** `SearchCoordinator` receives requests for tvdbId=370761 and tvdbId=83214 concurrently
- **THEN** two separate worker pipelines SHALL execute

#### Scenario: Movie requests coalesced by IMDB ID
- **WHEN** two `MovieSearchRequest`s with the same `ImdbId` arrive concurrently
- **THEN** only one pipeline SHALL execute

### Requirement: Receive + PipeTo pipeline orchestration
The `SearchCoordinator` SHALL use `Receive` (not `ReceiveAsync`) for all message handlers and `PipeTo` for async pipeline steps. The mailbox SHALL remain open between pipeline steps to enable coalescing.

#### Scenario: New requests processed during in-flight pipeline
- **WHEN** a pipeline is in-flight for tvdbId=370761 (waiting for MediathekGatewayWorker response)
- **THEN** `SearchCoordinator` SHALL be able to process new incoming requests (cache hits, new coalescing, different topics) without waiting

#### Scenario: Pipeline step completion triggers next step
- **WHEN** `ShowResolverWorker` replies with `ShowResolved` for an in-flight pipeline
- **THEN** `SearchCoordinator` SHALL advance the pipeline state and trigger the next step (Ask MediathekGatewayWorker)

### Requirement: Pipeline flow for TV search
For TV searches, `SearchCoordinator` SHALL execute this pipeline via Ask + PipeTo:
1. Ask `ShowResolverWorker` for TVDB resolution AND Ask `RuleSetCoordinator` for rules (parallel)
2. Ask `MediathekGatewayWorker` for Mediathek items
3. Ask `MatchWorker` to match items against rules
4. Ask `QualityProbeWorker` to probe matched results
5. Ask `ScoreWorker` to rank results

On completion: cache results, tell `MatchLedgerActor` the match record (fire & forget), reply to all pending callers with per-episode filtering.

#### Scenario: Parallel resolution of show info and rules
- **WHEN** a TV search pipeline starts for tvdbId=370761
- **THEN** `SearchCoordinator` SHALL ask `ShowResolverWorker` and `RuleSetCoordinator` concurrently, and proceed to step 2 only when both have replied

#### Scenario: Pipeline completes and replies to coalesced callers
- **WHEN** the pipeline completes with 5 matched results and 3 callers are pending (episodes 3, 4, 5)
- **THEN** each caller SHALL receive only the results matching their requested episode

### Requirement: Pipeline flow for movie search
For movie searches, `SearchCoordinator` SHALL execute: Ask `ShowResolverWorker` for TMDB resolution → Ask `MediathekGatewayWorker` → Ask `MatchWorker` → Ask `QualityProbeWorker` → Ask `ScoreWorker`. No rules step (movies use generic pipeline).

#### Scenario: Movie pipeline with TMDB resolution
- **WHEN** a `MovieSearchRequest` with imdbId="tt0108052" arrives
- **THEN** `SearchCoordinator` SHALL ask `ShowResolverWorker` for TMDB resolution, use the German title for the Mediathek query, and pass the runtime to `MatchWorker` for duration context

### Requirement: Pipeline flow for text search
For text searches, `SearchCoordinator` SHALL execute: Ask `MediathekGatewayWorker` → Ask `MatchWorker` → Ask `QualityProbeWorker` → Ask `ScoreWorker`. No show resolution or rules step.

#### Scenario: Text pipeline
- **WHEN** a `TextSearchRequest` with query="Tatort" arrives
- **THEN** `SearchCoordinator` SHALL skip show resolution and rules, query the Mediathek directly, and proceed through match → probe → score

### Requirement: Five permanent child workers
`SearchCoordinator` SHALL create five permanent child actors in `PreStart`: `ShowResolverWorker`, `MediathekGatewayWorker`, `MatchWorker`, `QualityProbeWorker`, `ScoreWorker`. Each worker SHALL be supervised with Restart strategy.

#### Scenario: Workers created at startup
- **WHEN** `SearchCoordinator` starts
- **THEN** it SHALL create all five workers as children before processing any search request

#### Scenario: Worker crash restarts only that worker
- **WHEN** `MatchWorker` throws an unhandled exception
- **THEN** `MatchWorker` SHALL be restarted and all other workers and the coordinator's cache/coalescing state SHALL remain unaffected

### Requirement: Dependency resolution for RuleSetCoordinator and MatchLedgerActor
`SearchCoordinator` SHALL resolve references to `RuleSetRegistryActor` (current name, renamed in Phase 3) and `MatchLedgerActor` at startup. It SHALL stash all requests until both are resolved and re-resolve if either terminates.

#### Scenario: Stashing until dependencies resolved
- **WHEN** `SearchCoordinator` starts and `RuleSetRegistryActor` has not yet been resolved
- **THEN** all incoming search requests SHALL be stashed

#### Scenario: Re-resolution on termination
- **WHEN** the resolved `RuleSetRegistryActor` terminates
- **THEN** `SearchCoordinator` SHALL re-resolve it and stash new requests until resolved

### Requirement: Match record forwarding
When a pipeline completes and produces a `MatchRecord`, `SearchCoordinator` SHALL tell `MatchLedgerActor` to record it (fire & forget). Workers SHALL NOT reference `MatchLedgerActor` directly.

#### Scenario: Match record forwarded after pipeline completion
- **WHEN** a TV search pipeline completes with a non-null `MatchRecord`
- **THEN** `SearchCoordinator` SHALL tell `MatchLedgerActor` with `RecordMatchResult`

### Requirement: Metrics unchanged
`SearchCoordinator` SHALL emit the same OpenTelemetry metrics as the current `SearchActor`: `funkarr_search_total`, `funkarr_search_duration_seconds`, `funkarr_cache_hit_total`.

#### Scenario: Search total incremented
- **WHEN** a search pipeline completes
- **THEN** `funkarr_search_total` SHALL be incremented with `type` and `outcome` tags
