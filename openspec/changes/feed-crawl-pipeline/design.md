## Context

The crawl talks to four actors (MediathekViewWeb manager, ruleset resolver, the Search evaluation regions, the feed store) and does pure work in between (window check, de-duplication, grouping, feed gate). The `MediathekViewWebManager` already uses Akka.Streams for its request queue.

## Goals / Non-Goals

**Goals:** a crawl that fetches and evaluates concurrently with backpressure, can be cancelled, reports failures per ruleset group, and never silently skips entries inside its window.

**Non-Goals:** splitting the crawl across nodes, progress reporting in the UI, changing the feed gate or ruleset matching.

## Decisions

### One stream per crawl, every step a stage
```
Source.Single(cursor)
  .SelectAsync(1, Probe)                       // size=1 query → ListTimestamp, window
  .SelectAsync(1, WithRuleSets)                // only when there is an update
  .ConcatMany(plan => Crawl(plan) | Probed)
      Crawl: Probed ++ Source.From(offsets)
        .SelectAsync(3, QueryItems)            // up to three pages in flight, order kept
        .Select(EvaluatePage)                  // window, entries still indexing, end detection
        .TakeWhile(!ReachedEnd, inclusive)     // stops prefetching at the first page reaching the cursor
        .StatefulSelectMany(DistinctAcrossPages)
        .SelectMany(Split)                     // 90-day cap, group per ruleset, PageCollected stats
        .SelectAsyncUnordered(MaxConcurrency, EvaluateGroup)   // evaluation + feed gate
        .Batch(100, …)                         // collects outcomes while a publish is in flight
        .SelectAsync(1, PublishBatch)          // PublishFeedEntries to the store
  .Recover(→ CrawlFailed)
  .ViaMaterialized(KillSwitch)
  .RunWith(Sink.Aggregate(FeedRunStats))       // → FeedRunOutcome
```
Stages exchange small `CrawlStep` records; the sink folds them into `FeedRunStats`, which produces the `FeedRunOutcome` the manager already understands. `FeedCrawlStages` holds the four dependencies as lambdas; `FromActors` builds them from `Ask` calls, tests pass plain functions.

### Grouping per page instead of across pages
Groups are formed per page, so a ruleset whose entries span several pages is evaluated more than once. A normal run needs one page (hourly Filmliste updates add a few dozen entries); only catch-up runs span pages, and those evaluations still run in parallel in the Search shard regions. `GroupBy` with time windows would avoid that at the cost of timers and harder tests.

### Failures
- MediathekViewWeb errors fail the stream; `Recover` turns them into `CrawlFailed`, so the outcome keeps the probed `ListTimestamp` for the three-failure rule. Queue-full answers are retried twice with backoff inside the query stage.
- A failing evaluation becomes `GroupFailed`; other groups continue. Any failed group or publish fails the run, the cursor stays and the next run repeats the window (the store ignores known entries).
- Cancellation: the manager aborts the kill switch and cancels the token in `PostStop`.

### Catch-up instead of a hard page limit
The old 20-page limit advanced the cursor past entries it never read. The window is now crawled completely, bounded by `MaxPages` (200 × 100 entries) as a safety limit and by `MaxCatchUp` (7 days) after long outages; both cases are reported as a gap. Backpressure keeps MediathekViewWeb load at what evaluation can process.

### Names
`FeedCrawlManager` (when and from where to crawl, cursor), `FeedCrawlPipeline` (one run), `FeedStoreManager` (published releases, queries, retention, cache invalidation), `PublishFeedEntries`, `StartFeedCrawl`, `QueryFeedCrawlStatus`.

## Risks / Trade-offs

- [Prefetching up to two pages past the end] → Bounded by the prefetch of three and cancelled by `TakeWhile`.
- [Repeated evaluation for rulesets spanning pages] → Only in catch-up runs; see above.
- [Catch-up runs take longer] → They run in the background between timer ticks; a run in progress blocks overlapping runs as before.
