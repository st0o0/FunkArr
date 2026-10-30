# Design

## Context

- RSS sync requests (`t=tvsearch|movie|search` without `q` and IDs) are answered with a validation item since #206 (`NewznabSearchService.IsWithoutSearchCriteria`, `ValidationRss`).
- `TvSearchWorker` / `MovieSearchWorker` (sharded per search) run query → ruleset resolution → scoring → enrichment → history → reply. After the query step they continue from `Apply(QueryMediathekCompleted)` + `TryGetRuleSetRequest`, which resolves the ruleset from `Sources[0].Topic`.
- `RuleSetResolver` answers `QueryRegisteredRuleSets` with topic, aliases and media type of every ruleset in one message.
- MediathekViewWeb facts (verified in its source, see #204): each newly added entry gets `filmlisteTimestamp` = timestamp of the Filmliste update that added it; `queryInfo.filmlisteTimestamp` is raised only after an update is fully indexed; `id` is a content hash and changes on edits; queries may return HTTP 503 while indexing.

See proposal.md for motivation.

## Goals / Non-Goals

**Goals:** reliable, gap-free RSS sync; feed releases identical to search releases; correctness in one place (dedupe in the feed store); responsive RSS answers during crawls.

**Non-Goals (follow-ups):** library-aware matching for topics without a ruleset; targeted re-checks per episode; a local Filmliste import; recording gate decisions as separate history records.

## Decisions

### Two actors, not three

Issue #204 sketched `FeedIngestManager`, a sharded `FeedWorker` per ruleset and `FeedManager`. Applying the rule "an actor only for its own state, lifecycle or failure boundary": a `FeedWorker` would hold no state. Evaluation already runs in sharded Search workers, and per-group failure isolation is a `try/catch` per group. So:

- **`FeedIngestManager`** (cluster singleton, `ReceivePersistentActor`, `IWithTimers`, persistence id `feed-ingest`): owns cursor, run scheduling and run status. A run is executed by `FeedCrawler`, a plain class whose async `RunAsync` is piped back to the manager (`PipeTo` with a `FeedCrawlFailed` failure message). The manager never blocks; it answers status queries during a run and ignores ticks while `_running`.
- **`FeedManager`** (cluster singleton, `ReceivePersistentActor`, persistence id `feed`): owns the published releases. It is the only writer, dedupes by stable ID, trims by retention on an hourly timer and on each add, and answers `QueryFeed`.
- **Pure logic** lives in state/static classes: `FeedIngestPlanner` (window and page evaluation), `RuleSetLookup` (topic/alias → ruleset), `FeedGate` (publish decision, stable ID, first seen), `FeedIngestState`, `FeedManagerState`.

### Cursor = last completed Filmliste update

The run probes with `size=1` to read L = `ListTimestamp`. Collect `cursor < ts ≤ L`; skip `ts > L` (update in progress). Since a completed update never grows, the cursor is a single number and needs no tie-breaking ID set. Offset paging over a live list can only produce duplicates (new entries push offsets down), never gaps; duplicates are removed per run by stable ID and again in the feed store.

First run: lower bound `L − InitialLookback`. `FirstSeenAt` is always the entry's `filmlisteTimestamp`, so the first run needs no special dating.

### Failure policy

A failed MVW query (503, queue full, error) or a failed group keeps the cursor, so the next run repeats the window. Successful groups are still published; the feed store dedupes the repeat. After three consecutive failed runs the cursor advances anyway with `Gap = true`, so one permanently failing ruleset cannot pin the window forever.

### Evaluation through the Search workers

New commands `EvaluateSeries(SearchId, Source, Items)` / `EvaluateMovies(...)` (`IWithSearchId`) go to the existing shard regions. The worker runs `Init`, `Apply(new QueryMediathekCompleted(items, n))` and then the same continuation as after a real query (extracted into one method), so ruleset resolution, scoring, enrichment, the variant policy and history recording are shared. They reply with the existing `SearchSeriesResponse` / `SearchMovieResponse`. This deviates from "each command owns its responses" on purpose: the evaluation *is* the same search, only its input is pre-fetched, and a separate response hierarchy would duplicate the reply and telemetry code.

Items of a group are matched to one ruleset by topic or alias, so `Sources[0].Topic` resolves to that ruleset in the worker.

### Gate and release identity

`FeedGate` keeps releases with `Score ≥ MinConfidence` and S/E (TV) or TMDB/IMDB ID (movie). Search results are expanded per video quality, so each quality variant is its own release. Its source entry is found by video URL. Stable ID = first 32 hex chars of SHA-256 over `channel|url`. It is independent of title and mapping, so ruleset updates do not create new GUIDs. Gate rejections are counted per reason in the `FunkArr.Feed` meter. Scoring decisions (including variant exclusions) are already in the match history with source `Feed`.

### RSS serving

`NewznabSearchService` keeps `IsWithoutSearchCriteria`. In that case it asks `IFeedManager` with the category (`tvsearch` → TV, `movie` → Movie, `search` → by `cat`, mixed → both), `offset` and `limit`. Items reuse the existing `SearchResultItem` → RSS item mapping, with GUID = stable ID and `pubDate = FirstSeenAt`. If `total = 0` for that category, or the feed manager does not answer within the timeout, the validation item is returned, so the indexer test passes on a fresh install.

### Persistence

Events in `FunkArr.Persistence/Events/Feed/`: `FeedCursorAdvanced(Cursor, CompletedAt, NewItems, Published, Unmatched, Gap)`, `FeedRunFailed(FailedAt, Reason, ConsecutiveFailures)`, `FeedEntriesAdded(PersistedFeedEntry[])`, `FeedEntriesExpired(string[] StableIds)`, plus persisted snapshots for both actors (snapshot every 50 events). `PersistedFeedEntry` stores the release fields needed for RSS (title, channel, topic, urls, duration, size, quality, air date, score, metadata, media type, stable ID, first seen).

### Hosting

New project `FunkArr.Feed` (references Core). It is registered in the host, the solution and `ArchitectureSpec` (domain isolation, sealed types). Two singletons in `AkkaSetupContainer`; `FeedOptions` in `FeedSetupContainer`; meter `FunkArr.Feed`.

## Risks / Trade-offs

- [MediathekViewWeb load] → One `size=1` probe per tick when nothing changed; full crawl only after an update (roughly hourly), bounded by `MaxPages × PageSize`.
- [Enrichment API load from feed evaluations] → Bounded by `MaxConcurrency`; only entries of known rulesets are evaluated.
- [Feed store growth] → Retention trim; snapshots every 50 events.
- [Search worker passivation during long groups] → Ask timeout of 60 s per group; a timeout counts as group failure (cursor kept).
- [Topics without ruleset are not published] → Counted and shown on the Feed page; library-aware matching is the planned follow-up.
- [MediathekViewWeb response cache (5 min)] → A probe may see the previous L for up to 5 minutes; the next tick catches up.

## Migration Plan

No migration: new persistence ids start empty, and the first run uses `InitialLookback`. Disable with `FunkArr__Feed__Enabled=false`. Rollback by revert. Persisted feed events are then simply unused.
