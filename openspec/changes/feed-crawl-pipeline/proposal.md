## Why

A feed run was one large `async` method (`FeedCrawler`) driven by `FeedIngestManager`: pages were fetched one after another, all entries collected first, then evaluated with `Parallel.ForEachAsync`, then published. The run could not be cancelled, nothing ran in parallel with fetching, and a hard limit of 20 pages silently skipped entries after a large Filmliste update or an outage. The names `FeedIngestManager` / `FeedManager` did not say which actor crawls and which one stores.

## What Changes

- Each crawl runs as one Akka.Streams pipeline (`FeedCrawlPipeline`): probe → load rulesets → pages with up to three in flight → window and end detection → de-duplication across pages → grouping per ruleset → parallel evaluation → feed gate → batched publishing → aggregated outcome. Every step is a stream stage; dependencies (MediathekViewWeb query, ruleset lookup, evaluation, publishing) are injected as lambdas (`FeedCrawlStages`).
- Backpressure: pages are only fetched as fast as evaluation keeps up; fetching stops as soon as a page reaches the cursor.
- The crawl can be cancelled (kill switch and cancellation token) when the manager stops.
- No silent gap: the window is crawled completely (safety limit `MaxPages` 200), and after an outage the crawl catches up on at most `MaxCatchUp` (7 days), reported as a gap.
- **Renames:** `FeedIngestManager` → `FeedCrawlManager`, `FeedManager` → `FeedStoreManager`, `AddFeedEntries` → `PublishFeedEntries`, `TriggerFeedCrawl` → `StartFeedCrawl`, `QueryFeedIngestStatus` → `QueryFeedCrawlStatus`, `FeedIngestPlanner` → `FeedCrawlPlanner`; persistence ids `feed-crawl` / `feed-store`.

## Capabilities

### New Capabilities

- `feed-crawl-pipeline`: how a feed crawl runs as a stream, when it stops, how failures and gaps are reported.

### Modified Capabilities

_None_ (the `rss-feed` change is not archived yet; its behaviour is kept, only the run mechanics and names change).

## Impact

- **Feed:** `FeedCrawlManager`, `FeedCrawlPipeline`, `FeedCrawlStages`, `FeedCrawlSteps`, `FeedCrawlPlanner`, `FeedStoreManager`; `FeedCrawler` removed.
- **Messages / Persistence / Core:** renamed types and actor keys; `FeedOptions.MaxCatchUp`, `MaxPages` default 200.
- **Api / ArrApi / Host:** use the renamed keys and messages; HTTP contracts unchanged.
- Version 0.x and #210 not merged yet, so the persistence id change needs no migration.
