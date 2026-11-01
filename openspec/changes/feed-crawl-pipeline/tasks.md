# Tasks

## 1. Renames

- [x] 1.1 Rename `FeedIngestManager` → `FeedCrawlManager`, `FeedManager` → `FeedStoreManager` (with states, actor keys, persistence ids `feed-crawl` / `feed-store`, host registration), `AddFeedEntries` → `PublishFeedEntries`, `TriggerFeedCrawl` → `StartFeedCrawl`, `QueryFeedIngestStatus` → `QueryFeedCrawlStatus`, `FeedIngestPlanner` → `FeedCrawlPlanner`, persisted states. Verify with `dotnet build` and no remaining old names in code.

## 2. Pipeline

- [x] 2.1 Add `FeedCrawlStages` (lambdas, `FromActors`), `FeedCrawlSteps` (`CrawlStep` records, `FeedRunStats`) and `FeedCrawlPipeline` (stream as designed, kill switch, queue-full retry). Verify with pipeline tests (unchanged, publish and unmatched, failing group, failing publish, queue full, page limit gap, catch-up gap, bounded prefetch, cancel).
- [x] 2.2 Add `CrawlWindow` with `MaxCatchUp` truncation and `DistinctAcrossPages` to `FeedCrawlPlanner`; add `FeedOptions.MaxCatchUp`, raise `MaxPages` to 200. Verify with planner tests.
- [x] 2.3 Run crawls from `FeedCrawlManager` through the pipeline, cancel in `PostStop`, record seen/unmatched telemetry from the outcome; remove `FeedCrawler`. Verify with `dotnet build` and existing state tests.

## 3. Docs and verification

- [x] 3.1 Update how-it-works, configuration (both languages) and AGENTS.md.
- [ ] 3.2 Run `dotnet format --verify-no-changes` and all test projects via CI and verify they pass.
