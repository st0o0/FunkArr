## Context

The crawl talks to four actors (MediathekViewWeb manager, ruleset resolver, the Search evaluation regions, the feed store) and does pure work in between (window check, de-duplication, grouping, feed gate). The `MediathekViewWebManager` already uses Akka.Streams for its request queue.

## Goals / Non-Goals

**Goals:** a crawl that fetches and evaluates concurrently with backpressure, can be cancelled, reports failures per ruleset group, and never silently skips entries inside its window.

**Non-Goals:** splitting the crawl across nodes, progress reporting in the UI, changing the feed gate or ruleset matching.

## Decisions

### One long-lived stream, owned by the manager
Like the `MediathekViewWebManager`, the `FeedCrawlManager` materializes its stream once in the constructor and talks to it through a `Source.Queue` and `Sink.ActorRef(Self)`:
```
Source.Queue<FeedCrawlPlan>(1)
  .Via(Flow.Create<FeedCrawlPlan>().ConcatMany(plan =>
      Source.From(plan.PageOffsets)
        .SelectAsync(3, offset => Ask MediathekViewWebManager)   // pages in flight, order kept
        .WireTap(page → FeedPageCrawled)
        .TakeWhile(!ReachedEnd, inclusive)                       // stops prefetching at the cursor
        .SelectMany(page.Collected)                              // inside the window
        .Where(WithinLead)                                       // 90-day cap
        .StatefulSelectMany(DistinctByStableId)                  // across pages, per run
        .WireTap(→ FeedItemCollected)
        .SelectMany(Match)                                       // ruleset by topic/alias
        .WireTap(→ FeedItemMatched)
        .GroupBy(500, ruleSetId).GroupedWithin(50, 1 s).MergeSubstreams()
        .SelectAsyncUnordered(MaxConcurrency, Ask TvEvaluation/MovieEvaluation region)
        .Select(Gate)
        .GroupedWithin(200, 2 s)
        .SelectAsync(1, Ask FeedStoreManager → FeedBatchPublished)
        .Concat(FeedCrawlFinished)
        .Recover(→ FeedCrawlFailed)))
  .To(Sink.ActorRef(Self))
```
The probe (one `size=1` query) and the ruleset lookup are actor `Ask`/`PipeTo` steps that decide whether a crawl is needed and build the `FeedCrawlPlan` (run id, window, lookup, limits). The actor folds the events into `FeedCrawlRun` and, on `FeedCrawlFinished`/`FeedCrawlFailed`, decides the cursor event as before. `GroupBy` lives inside the per-crawl source, so substreams complete with the crawl and never accumulate. The stream stops with the actor; events carrying an outdated run id are ignored.

### Failures
- A failed MediathekViewWeb page fails that crawl's source; `Recover` turns it into `FeedCrawlFailed` and the long-lived stream keeps running. The outcome keeps the probed `ListTimestamp` for the three-failure rule.
- A failing evaluation becomes a gated group with an error; other groups continue. Any failed group or publish fails the run, the cursor stays and the next run repeats the window (the store ignores known entries).
- Probe or ruleset failures finish the run as failed without touching the stream.

### Catch-up instead of a hard page limit
The old 20-page limit advanced the cursor past entries it never read. The window is now crawled completely, bounded by `MaxPages` (200 × 100 entries) as a safety limit and by `MaxCatchUp` (7 days) after long outages; both cases are reported as a gap. Backpressure keeps MediathekViewWeb load at what evaluation can process.

### Names
`FeedCrawlManager` (when and from where to crawl, cursor, the crawl stream), `FeedCrawlPlan` (one crawl's parameters and pure stage functions), `FeedCrawlRun` (one crawl's progress), `FeedStoreManager` (published releases, queries, retention, cache invalidation), `PublishFeedEntries`, `StartFeedCrawl`, `QueryFeedCrawlStatus`.

## Risks / Trade-offs

- [Prefetching up to two pages past the end] → Bounded by the prefetch of three and cancelled by `TakeWhile`.
- [Grouping windows add latency] → At most one second per ruleset group and two seconds per publish batch; both flush as soon as a crawl's source completes.
- [Catch-up runs take longer] → They run in the background between timer ticks; a run in progress blocks overlapping runs as before.
