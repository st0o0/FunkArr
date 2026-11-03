## Context

The FeedCrawlWorker is a child actor of FeedCrawlManager with 6 Become phases:
Probe → LoadRuleSets → Harvest → Evaluate → Publish → Complete. During Harvest,
a pool of FeedPageActors fetches pages concurrently from MediathekViewWeb. The
current control flow in `HandlePageResult` has two exit paths when `PendingPages`
hits 0: one via `StartEvaluating()` (success branch) and one via
`Complete(GetCrawlResult())` (error branch). Which path runs depends on whether
the last message to arrive was a successful or failed page — a race condition.

MVW returns HTTP 500 intermittently (observed in every E2E run). The Polly
Standard Resilience handler on the MediathekClient already retries, so by the
time a page error reaches the worker it's a confirmed failure. Current defaults
create 100+ requests per crawl (PageSize=100, MaxPages=200); MVW supports up to
1000 items per request.

## Goals / Non-Goals

**Goals:**
- Eliminate the race condition: evaluation always runs when items were collected
- Items from successful pages are never discarded due to failures on other pages
- Cursor only advances when items were actually processed
- Reduce MVW request volume by 10x through larger page sizes

**Non-Goals:**
- Feed-level retry logic (Polly already handles HTTP retries)
- Making RuleSetResolver persistent (registrations work correctly)
- Feed UI changes (existing UI handles all outcome types)
- Changing the FeedPageActor or FeedEvalActor

## Decisions

### 1. Unified PendingPages==0 path (over separate error/success branches)

`HandlePageResult` currently branches on `page.Error` and reaches `PendingPages==0`
in two different code paths. The fix merges them: page errors only stop new page
dispatch, they never prevent evaluation.

```
HandlePageResult(page):
  ApplyPageResult(page)
  if page OK and more pages available → fire next page
  if PendingPages == 0:
    if CollectedItems > 0 → StartEvaluating()
    else                  → Complete(Failed)
```

Alternative considered: check `state.PageError` instead of `page.Error` for the
eval decision. Rejected because it still leaves a branch — the simplest fix is to
remove the branch entirely and always evaluate when items exist.

### 2. CompletedPartial status (over reusing Completed with gap flag)

Adding a distinct `CrawlStatus.CompletedPartial` rather than overloading
`Completed` with `gap=true`. The manager's `HandleResult` already switches on
status — a new case is cleaner than checking both status and gap to determine
cursor behavior.

`GetCrawlResult` logic:

| PageError | CollectedItems | FailedGroups | Status |
|-----------|---------------|--------------|--------|
| null | any | 0 | Completed |
| null | any | >0 | Failed |
| set | >0 | any | CompletedPartial |
| set | 0 | n/a | Failed |

### 3. CompletedPartial always advances cursor (over retry-without-advance)

When a partial harvest is evaluated, the cursor advances to `listTimestamp` with
`gap=true`. The consecutive failure counter resets. This matches the principle
"we processed what we could." The gap flag tells downstream consumers (Feed UI,
RSS sync) that coverage is incomplete.

Alternative: don't advance cursor on partial, retry the full window next cycle.
Rejected because the window includes items we already published — re-processing
them would create duplicates (the FeedStoreManager deduplicates by stableId, but
the scoring work would repeat).

### 4. PageSize 1000, MaxPages 20, Prefetch 2

MVW returns at most 1000 items per request regardless of the requested `size`.
Verified empirically: `size=5000` returns 1000 items. Changing defaults to
`PageSize=1000, MaxPages=20` gives the same 20,000-item ceiling with ~10 requests
instead of ~200. `PagePrefetch=2` reduces concurrent pressure on MVW.

## Risks / Trade-offs

**Partial harvest may miss items from failed pages** → Accepted. The gap flag
records this. The alternative (discarding everything) is strictly worse. Next
crawl cycle picks up new items; the missed window may overlap if MVW's filmliste
timestamp advances slowly.

**Larger page responses increase per-request memory** → Negligible. 1000
`MediathekApiItem` records at ~500 bytes each ≈ 500 KB per page. The current
100-item pages already accumulate 10,000 items in `CollectedItems` (same total).

**`CompletedPartial` is a new persistence event variant** → No. The
`FeedCursorAdvanced` persistence DTO doesn't carry the status. It stores cursor,
newItems, published, unmatched, gap. `CompletedPartial` only affects the in-memory
`HandleResult` routing, then maps to `FeedCursorAdvanced` with `gap=true`. No
persistence schema change.
