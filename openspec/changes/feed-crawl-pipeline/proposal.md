## Why

A feed run was one large `async` method (`FeedCrawler`) driven by `FeedIngestManager`: pages were fetched one after another, all entries collected first, then evaluated with `Parallel.ForEachAsync`, then published. The run could not be cancelled, nothing ran in parallel with fetching, and a hard limit of 20 pages silently skipped entries after a large Filmliste update or an outage. The names `FeedIngestManager` / `FeedManager` did not say which actor crawls and which one stores.

## What Changes

- The `FeedCrawlManager` owns one long-lived Akka.Streams graph, materialized once like the `MediathekViewWebManager` stream: `Source.Queue<FeedCrawlPlan>` → `ConcatMany(crawl)` → `Sink.ActorRef(Self)`. Each crawl is an item flow: pages with up to three in flight → end detection → entries in the window → 90-day cap → de-duplication across pages → ruleset match → `GroupBy` ruleset with `GroupedWithin` → parallel evaluation → feed gate → `GroupedWithin` batches → publishing. The `Ask` calls to MediathekViewWeb, the Search evaluation regions and the store sit directly in the stage lambdas.
- The actor keeps the run state (`FeedCrawlRun`): it probes and loads rulesets, offers the plan, and folds the events the stream sends back (pages, collected and matched entries, published batches, finished/failed) into the outcome.
- Backpressure: pages are only fetched as fast as evaluation keeps up; fetching stops as soon as a page reaches the cursor.
- The stream lives and dies with the manager; results of older runs are ignored by run id.
- No silent gap: the window is crawled completely (safety limit `MaxPages` 200), and after an outage the crawl catches up on at most `MaxCatchUp` (7 days), reported as a gap.
- **Renames:** `FeedIngestManager` → `FeedCrawlManager`, `FeedManager` → `FeedStoreManager`, `AddFeedEntries` → `PublishFeedEntries`, `TriggerFeedCrawl` → `StartFeedCrawl`, `QueryFeedIngestStatus` → `QueryFeedCrawlStatus`, `FeedIngestPlanner` → `FeedCrawlPlanner`; persistence ids `feed-crawl` / `feed-store`.

## Capabilities

### New Capabilities

- `feed-crawl-pipeline`: how a feed crawl runs as a stream, when it stops, how failures and gaps are reported.

### Modified Capabilities

_None_ (the `rss-feed` change is not archived yet; its behaviour is kept, only the run mechanics and names change).

## Impact

- **Feed:** `FeedCrawlManager` (stream + run handling), `FeedCrawlPlan`, `FeedCrawlRun`, `FeedCrawlPlanner`, `FeedStoreManager`; `FeedCrawler` removed.
- **Messages / Persistence / Core:** renamed types and actor keys; `FeedOptions.MaxCatchUp`, `MaxPages` default 200.
- **Api / ArrApi / Host:** use the renamed keys and messages; HTTP contracts unchanged.
- Version 0.x and #210 not merged yet, so the persistence id change needs no migration.
