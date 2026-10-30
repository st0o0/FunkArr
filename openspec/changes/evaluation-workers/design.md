## Context

Before this change a search worker ran query → ruleset resolution → scoring → enrichment → history → reply as one state machine. The feed crawler entered that state machine halfway by sending pre-fetched items. Search entity IDs are the search ID, so a feed evaluation occupied a search entity with a fresh GUID and answered with `SearchSeriesCompleted`.

MediathekViewWeb filters `timestamp <= now + 1h` when `future=false`. With `future=true` the newest entries by `timestamp` are the ones furthest in the future, so a size-50 window sorted by `timestamp desc` is dominated by upcoming entries for daily shows.

## Goals / Non-Goals

**Goals:**
- One place that turns Mediathek entries into releases, used by search and feed alike.
- Feed evaluations enrich episodes and movies whenever the ruleset declares media IDs.
- Pre-released entries are added to search results without displacing aired entries.

**Non-Goals:**
- Changing the scoring, enrichment or release-title rules.
- Paging deeper into MediathekViewWeb or changing cache behaviour.

## Decisions

### Evaluation workers in their own shard regions
`TvEvaluationWorker` / `MovieEvaluationWorker` are sharded entities keyed by the evaluation ID (`IWithSearchId.SearchId` of the `Evaluate*` command, a fresh GUID per evaluation), passivated after 30 s via `ShardOptions`. Sharding keeps evaluations spread across the cluster like searches and keeps the search entity free for its own request. An actor pool under a singleton was considered but would funnel every evaluation through one node and break the per-request entity pattern the rest of Search uses.

### Search workers delegate after the query
The search worker resolves the ruleset only when the request has media IDs (needed to know the topic before querying). Text searches query first and leave topic-based resolution to the evaluation worker. The worker asks the evaluation region (`Ask`, 40 s timeout, below the 45 s controller timeout) and maps `Evaluate*Completed` to `Search*Completed` with its own search ID. A search with zero Mediathek entries replies empty without an evaluation.

### Own response types for evaluations
`Evaluate*` commands now own `Evaluate*Completed` / `Evaluate*Failed`, following the message naming convention. The earlier shortcut (reusing search responses) is no longer needed because the evaluation worker has its own reply path and telemetry.

### Resolved ruleset travels with the command
`Evaluate*` carries an optional `RuleSetResolved`. When the search worker already resolved by ID, the evaluation worker skips resolution; otherwise it resolves from the topic of the first entry. Media IDs and season travel along so enrichment works for both sources.

### Feed passes registered media IDs
`RuleSetLookup` groups feed items per ruleset and keeps the ruleset's `ExternalIds`. The crawler sends `EvaluateSeries(…, TvdbId, ImdbId)` / `EvaluateMovies(…, ImdbId, TmdbId)`. Rulesets without IDs still evaluate, just without enrichment.

### Pre-released entries handled by the Mediathek manager
An interim version sent a `future=false` and a `future=true` query from every search worker and merged them there. It was replaced by adaptive paging inside `MediathekViewWebManager` (see `include-prereleased-entries`), so search workers send one `QueryMediathek` and receive one response.

### Evaluation lives in the Feed domain
The release pipeline (evaluation workers, `SourceInfo`, `SceneRelease`, `ReleaseDisplay`, `EnrichedItem`, `VariantDetector`, …) moves from `FunkArr.Search` to `FunkArr.Feed`. After the split, Search only queries MediathekViewWeb; nothing in Search used these types except the evaluation itself. Search reaches the evaluation through `ITvEvaluationRegion` / `IMovieEvaluationRegion` in Core and the `Evaluate*` messages, so no project reference between the domains is needed. The feed crawler talks to the same regions. A separate `FunkArr.Evaluation` project was considered but adds a project for what is the core of the feed anyway.

## Risks / Trade-offs

- Two Mediathek requests per search double the load on the bounded Mediathek queue → responses are cached for 5 minutes and queue-full is retried with backoff; the option can be disabled.
- One more hop (search → evaluation entity) adds latency → negligible compared to MediathekViewWeb round trips.
- An evaluation that outlives the search ask (40 s) is wasted work → it still records history, and the entity passivates.
