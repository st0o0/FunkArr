## Why

A single MVW HTTP 500 during feed harvest causes all successfully-collected items to be
discarded without evaluation. The `FeedCrawlWorker.HandlePageResult` state machine has a
race condition: when the failed page message arrives last, `StartEvaluating()` is never
called and thousands of items are thrown away. After 3 consecutive failures the cursor
advances past the data window — those items are permanently lost. This triggers reliably
because MVW is intermittently unstable (observed in every E2E run). Additionally, the
current page size of 100 creates 100+ HTTP requests per crawl; MVW supports up to 1000
items per request, so we're doing 10x more requests than necessary.

## What Changes

- Fix `HandlePageResult` to always call `StartEvaluating()` when `PendingPages == 0`,
  regardless of whether the last page had an error
- Add `CompletedPartial` crawl status for crawls where some pages failed but collected
  items were still evaluated and published
- Revise cursor advancement: `CompletedPartial` with collected items advances cursor
  (gap=true); without collected items it counts as a regular failure
- Fix `GetCrawlResult` unmatched calculation: report `unmatched=0` when evaluation was
  skipped (no items), not `unmatched=collected`
- Increase MVW page size from 100 to 1000 (MVW maximum), reduce `MaxPages` from 200
  to 20, reduce `PagePrefetch` from 3 to 2 — same item ceiling, 10x fewer requests

## Capabilities

### New Capabilities

- `feed-partial-harvest`: Defines behavior when a feed crawl partially succeeds —
  some MVW pages fail but collected items are still evaluated, published, and the
  cursor advances with a gap marker

### Modified Capabilities

- `feed-crawl-lifecycle`: Adds `CompletedPartial` outcome and changes cursor
  advancement rules
- `feed-page-fetch`: Changes default page size from 100 to 1000 and prefetch from
  3 to 2

## Impact

- `FeedCrawlWorker.cs` — `HandlePageResult` control flow, remove early-return on error
- `FeedCrawlWorkerState.cs` — `GetCrawlResult` outcome logic and unmatched calculation
- `CrawlResult.cs` — add `CompletedPartial` to `CrawlStatus` enum
- `FeedCrawlManager.cs` — `HandleResult` switch for new `CompletedPartial` status
- `FeedOptions.cs` — default value changes for `PageSize`, `MaxPages`, `PagePrefetch`
- No API changes, no persistence schema changes, no UI changes
