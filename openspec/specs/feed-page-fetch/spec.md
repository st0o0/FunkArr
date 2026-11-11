# Feed Page Fetch

Sequential page fetch via Source.UnfoldAsync loop.

## Fetch Strategy

Sequential page fetch via `Source.UnfoldAsync`:
- One page at a time, offset-based pagination
- Default PageSize=1000, MaxPages=20 (from FeedOptions)
- Pages sorted by filmlisteTimestamp desc (newest first, walk backward)

### Why Sequential

The MediathekViewWebManager has an internal `Source.Queue` with
`maxConcurrent=3`. Regardless of how many concurrent Asks are sent,
only 3 HTTP requests execute in parallel. The previous pool (4 actors)
and stream dispatch added overhead without additional throughput.

Sequential fetch also enables correct early termination: items are sorted
by timestamp, so as soon as an item's timestamp <= lower bound, we know
all remaining items are older — `ReachedEnd = true`.

Normal operation (15-min interval): 1-3 pages, <1s total.
Catch-up after downtime: up to 20 pages.

## Page Filtering

`FilterPage(response, ctx)` is a private static method on FeedHarvestActor:

1. Extract items from QueryMediathekCompleted response
2. Filter to window: `LowerBound < filmlisteTimestamp <= UpperBound`
3. Skip items without any video URL (UrlVideoHd/UrlVideo/UrlVideoLow)
4. Skip items with timestamp > lead limit (90 days ahead)
5. Deduplicate by StableId (SHA256 of channel + URL)
6. Set `ReachedEnd = true` if any item has timestamp <= LowerBound
   or if returned items < PageSize

Returns `(MediathekItem[] items, bool reachedEnd)`.

## Topic Grouping

`GroupByTopic(items, targets)` is a private static method on FeedHarvestActor:

1. For each item, look up `item.Topic` in the target map
2. Group matched items by RuleSetId
3. Items without a matching topic are counted as unmatched
4. Return `IReadOnlyList<ItemGroup>`

## Deleted

- SmallestMailboxPool dispatch
- FeedCrawlPageActor fetch logic
- MaxConcurrency / PagePrefetch options (no longer used)
- Pool resize via AdjustPoolSize
