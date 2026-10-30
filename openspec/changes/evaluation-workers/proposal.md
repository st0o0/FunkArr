## Why

The search workers (`TvSearchWorker`, `MovieSearchWorker`) own two jobs: querying MediathekViewWeb and evaluating the result (ruleset resolution, scoring, enrichment, history). The RSS feed (#204) reused them by sending `EvaluateSeries` / `EvaluateMovies` into the search shard regions, which mixed feed evaluations into search entities, borrowed the search response types, and skipped enrichment because feed evaluations carried no TVDB/IMDB/TMDB IDs.

A live check of `future=true` (#205) also showed a regression: topic searches return the newest 50 entries, and with `future=true` many of those slots are taken by entries scheduled weeks ahead (e.g. 48 of 50 for a daily show), pushing aired episodes out of the window.

## What Changes

- New sharded `TvEvaluationWorker` / `MovieEvaluationWorker` in their own shard regions (`tv-evaluation`, `movie-evaluation`) own the evaluation pipeline: ruleset resolution from the topic (when not already resolved), scoring with the variant policy, enrichment, scoring history and release generation.
- `EvaluateSeries` / `EvaluateMovies` get their own response types (`EvaluateSeriesCompleted` / `EvaluateSeriesFailed`, `EvaluateMoviesCompleted` / `EvaluateMoviesFailed`) and carry optional media IDs, season and an already resolved ruleset.
- The search workers only resolve the ruleset by media ID, query MediathekViewWeb (with queue-full retries) and delegate the entries to the evaluation region.
- Searches always send two MediathekViewWeb queries (`future=false` and `future=true`, otherwise identical) and merge them, de-duplicated by video URL. This supersedes the single-query wording of the `prereleased-entries` requirement.
- The feed crawler sends its ruleset groups to the evaluation regions and passes the ruleset's registered media IDs so feed entries are enriched like search results.
- The evaluation pipeline (evaluation workers and states, `SourceInfo`, `SceneRelease`, `ReleaseDisplay`, `EnrichedItem`, `MediaIdentity`, `MatchInfo`, `VideoQuality`, `VariantDetector`) lives in the `FunkArr.Feed` domain; `FunkArr.Search` keeps querying only.
- New metrics `funkarr.feed.evaluations_total` and `funkarr.feed.evaluation_duration_seconds`.

## Capabilities

### New Capabilities

- `search-evaluation`: dedicated evaluation workers, delegation from search workers, feed evaluation with media IDs, and the two-query merge for pre-released entries.

### Modified Capabilities

- `prereleased-entries` (from the not yet archived `include-prereleased-entries` change): the requirement is rewritten in place to the two-query behaviour without a configuration option.

## Impact

- Code: `FunkArr.Messages/Search`, `FunkArr.Core/ActorKeys`, `FunkArr.Search` (slimmed search states and workers, query merge, telemetry), `FunkArr.Feed` (evaluation workers and release pipeline moved from Search, crawler, ruleset lookup), host shard registration.
- Tests: search worker and state tests split into search and evaluation tests; feed crawler tests.
- Docs: how-it-works, observability, AGENTS.md actor topology.
- No persistence or API contract changes. Version 0.x, no migration.
