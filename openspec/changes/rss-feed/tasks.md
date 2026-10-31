# Tasks

## 1. Messages and MediathekViewWeb mapping

- [x] 1.1 Map `filmlisteTimestamp` per item and in `queryInfo` with a lenient converter (number, numeric string, empty, missing); carry `MediathekItem.FilmlisteTimestamp` and `QueryMediathekCompleted.ListTimestamp`. Verify with model tests for each value form.
- [x] 1.2 Add `SearchSource.Feed` with persisted, history-mapping, API and UI counterparts. Verify with the existing enum parity/mapping tests extended for `Feed`.
- [x] 1.3 Add feed messages in `FunkArr.Messages/Feed` (`FeedEntry`, `AddFeedEntries`, `QueryFeed`, `QueryFeedIngestStatus`, `TriggerFeedCrawl`) and `EvaluateSeries` / `EvaluateMovies` in `FunkArr.Messages/Search`. Verify with `dotnet build`.

## 2. Search evaluation entry point

- [x] 2.1 Handle `EvaluateSeries` / `EvaluateMovies` in the TV and movie workers by applying the items and continuing like after a query (shared continuation method). Verify with worker tests that an evaluation resolves the ruleset from the items' topic and sends no Mediathek query.

## 3. Feed domain

- [x] 3.1 Create `FunkArr.Feed` and `FunkArr.Feed.Tests`, add them to the solution, host and `ArchitectureSpec`; add `FeedOptions`, marker interfaces and meter. Verify with `dotnet build` and the architecture tests.
- [x] 3.2 Implement `FeedIngestPlanner` (window, page evaluation, dedupe, gap) and `RuleSetLookup`. Verify with unit tests for every ingest scenario of the spec.
- [x] 3.3 Implement `FeedGate` (score, S/E or IDs, stable ID, first seen). Verify with unit tests.
- [x] 3.4 Implement `FeedManagerState` and `FeedManager` (dedupe keeping first `FirstSeenAt`, retention, ordering, category filter, paging, persistence). Verify with state tests.
- [x] 3.5 Implement `FeedIngestState`, `FeedCrawler` and `FeedIngestManager` (timer, no overlap, cursor advance only on success, three-failure rule, status query, manual crawl). Verify with state tests and a crawler test using test probes.
- [x] 3.6 Register both singletons and `FeedSetupContainer` in the host. Verify with `dotnet build`.

## 4. RSS, API, UI, docs

- [x] 4.1 Serve RSS-sync requests from `IFeedManager` with GUID = stable ID and `pubDate = FirstSeenAt`, falling back to the validation item when the feed is empty or unavailable. Verify with unit tests for the mapping and integration tests for feed and fallback.
- [x] 4.2 Add `GET /api/feed` and `POST /api/feed/crawl`. Verify with integration tests.
- [x] 4.3 Add the Feed page (entries, ingest status, crawl button), navigation entry and en/de translations. Verify the frontend CI check passes.
- [x] 4.4 Document the feed (configuration and how-it-works, en + de), replacing the interim validation-item note. Verify both languages match.
- [x] 4.5 Run `dotnet format --verify-no-changes` and all test projects via CI and verify they pass.

## Workflow follow-up

- Open the PR stacked on `feat/include-future` (#208), closing #204.
- Manual e2e check in the dev environment: Sonarr RSS sync grabs a new episode of a monitored show with a community ruleset.
