## Why

The feed crawl pipeline silently loses data under multiple failure conditions. Five
related bugs were found during code review, all in the FeedCrawlManager and
FeedHarvestActor. Together they mean: (a) page-level errors drop items without any
record, (b) MVW outages are misreported as "unchanged" so monitoring never fires,
and (c) the consecutive-failure cursor-advance safety net is unreachable, so failures
repeat endlessly instead of moving past a problematic window.

## What Changes

- Fix the unreachable gap-advance branch in FeedCrawlManager so that 3 consecutive
  failures actually advance the cursor past the stuck window
- Make FeedHarvestActor report page-level errors (MVW 503, QueueFull) as stream
  failures instead of silently advancing the cursor past lost items
- Set the Truncated flag when MaxPages caps a harvest, so the Manager records a
  CompletedWithGap instead of a silent Completed
- Classify a non-Completed probe response as HarvestFailed (not HarvestUnchanged)
  so that ConsecutiveFailures increments and monitoring reflects the real state
- Stop treating QueueFull during page fetch as end-of-data — retry or fail the
  stream instead of silently dropping the remaining window
- Persist HarvestUnchanged outcomes so LastRunAt and last outcome survive restarts

## Capabilities

### New Capabilities

- `feed-crawl-error-classification`: Rules for how each harvest-stage failure
  (probe, page-fetch, evaluation, publish) maps to HarvestCompleted / HarvestFailed /
  HarvestUnchanged, and which failures increment ConsecutiveFailures.

### Modified Capabilities

- `feed-crawl-lifecycle`: Cursor-advance-on-failure branch must receive a non-zero
  ListTimestamp; HarvestUnchanged must be persisted.
- `feed-harvest-pipeline`: FilterPage must fail the stream on non-Completed responses
  instead of returning ([], reachedEnd:true); MaxPages cap must set Truncated.
- `feed-partial-harvest`: Update to reflect that page failures now fail the stream
  (matching the documented "simpler, consistent" option already described in the spec).

## Impact

- **Code**: `FeedCrawlManager.cs` (failure handler, persistence of Unchanged),
  `FeedHarvestActor.cs` (FilterPage, probe classification, MaxPages truncation),
  `FeedCrawlManagerState.cs` (new persistence event for Unchanged runs).
- **Persistence**: New event type `FeedUnchangedRun` (or extend existing). Existing
  events and snapshots are unaffected (extend-only).
- **Telemetry**: ConsecutiveFailures metric will now correctly increment on MVW
  outages. Existing `funkarr.feed.probes_unchanged` counter semantics change (no
  longer includes probe failures).
- **Tests**: FeedCrawlManager and FeedHarvestActor tests need new cases for every
  failure classification path.
- **No API changes, no UI changes, no breaking changes to external consumers.**
