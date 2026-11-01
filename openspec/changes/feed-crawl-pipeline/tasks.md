# Tasks

## 1. Renames

- [x] 1.1 Rename `FeedIngestManager` → `FeedCrawlManager`, `FeedManager` → `FeedStoreManager` (with states, actor keys, persistence ids `feed-crawl` / `feed-store`, host registration), `AddFeedEntries` → `PublishFeedEntries`, `TriggerFeedCrawl` → `StartFeedCrawl`, `QueryFeedIngestStatus` → `QueryFeedCrawlStatus`, `FeedIngestPlanner` → `FeedCrawlPlanner`, persisted states. Verify with `dotnet build` and no remaining old names in code.

## 2. Crawl stream

- [x] 2.1 Materialize one long-lived stream in `FeedCrawlManager` (`Source.Queue<FeedCrawlPlan>` → `ConcatMany` crawl → `Sink.ActorRef`) with `Ask` calls directly in the stage lambdas, `GroupBy` + `GroupedWithin` per ruleset and batched publishing. Verify with manager tests driving the stream through probes (unchanged, full crawl, failed group, failed page, overlapping start).
- [x] 2.2 Add `FeedCrawlPlan` (offsets, queries, read, lead cap, match, group, evaluation messages, gate) and `FeedCrawlRun` (event folding, outcome). Verify with plan and run tests.
- [x] 2.3 Add `CrawlWindow` with `MaxCatchUp` truncation and `DistinctByStableId` to `FeedCrawlPlanner`; add `FeedOptions.MaxCatchUp`, raise `MaxPages` to 200. Verify with planner tests.
- [x] 2.4 Remove `FeedCrawler`, the per-run pipeline and the per-page ruleset grouping. Verify no references remain.

## 3. Docs and verification

- [x] 3.1 Update how-it-works, configuration (both languages) and AGENTS.md.
- [ ] 3.2 Run `dotnet format --verify-no-changes` and all test projects via CI and verify they pass.
