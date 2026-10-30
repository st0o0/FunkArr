# Tasks

## 1. Messages and wiring

- [x] 1.1 Give `EvaluateSeries` / `EvaluateMovies` optional media IDs, season (series) and `RuleSetResolved`, plus own `Evaluate*Completed` / `Evaluate*Failed` responses. Add `ITvEvaluationRegion` / `IMovieEvaluationRegion`. Verify with `dotnet build`.
- [x] 1.2 Register the `tv-evaluation` / `movie-evaluation` shard regions with `ShardMessageExtractor` and 30 s passivation. Verify with `dotnet build`.

## 2. Evaluation workers

- [x] 2.1 Add `TvEvaluationState` / `MovieEvaluationState` (ruleset, scoring, enrichment, history, releases) and `TvEvaluationWorker` / `MovieEvaluationWorker`. Verify with evaluation state and worker tests (topic resolution, resolved ruleset, no ruleset, scoring failure, enrichment success/failure, empty input).
- [x] 2.2 Add `funkarr.feed.evaluations_total` / `funkarr.feed.evaluation_duration_seconds`. Verify the docs list them.
- [x] 2.3 Move the evaluation pipeline and its tests from `FunkArr.Search` to `FunkArr.Feed` and register the evaluation regions with the Feed actors. Verify no Search type references remain in Feed (and vice versa) and the architecture tests pass.

## 3. Search workers

- [x] 3.1 Slim `TvSearchWorkerState` / `MovieSearchWorkerState` to ID-based ruleset resolution, query building and delegation. Verify with search state tests.
- [x] 3.2 Make the search workers delegate to the evaluation regions and keep queue-full retries. Verify with worker tests (text search, ID search, empty result, evaluation failure, retries).

## 4. Feed

- [x] 4.1 Keep ruleset media IDs in `RuleSetGroup` and send groups to the evaluation regions with those IDs. Verify with `RuleSetLookupTests` and `FeedCrawlerTests`.

## 5. Docs and verification

- [x] 5.1 Update how-it-works, observability (both languages) and the AGENTS.md actor topology and meter table.
- [x] 5.2 Run `dotnet format --verify-no-changes` and all test projects via CI and verify they pass.

## Workflow follow-up

- Open the PR stacked on `feat/rss-feed` (#209), referencing #204 and #205.
