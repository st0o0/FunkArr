## 1. CrawlStatus enum + CrawlResult

- [x] 1.1 Add `CompletedPartial` to `CrawlStatus` enum in `CrawlResult.cs`
- [x] 1.2 Add `CrawlResult.ForCompletedPartial` factory method (same shape as `ForCompleted` but with `CompletedPartial` status)

## 2. FeedCrawlWorker — fix HandlePageResult

- [x] 2.1 Refactor `HandlePageResult` to remove the error-branch `Complete()` call — page errors stop new page dispatch but never skip evaluation
- [x] 2.2 When `PendingPages == 0`: call `StartEvaluating()` if `CollectedItems.Count > 0`, else `Complete(Failed)`

## 3. FeedCrawlWorkerState — fix GetCrawlResult

- [x] 3.1 Update `GetCrawlResult` to return `CompletedPartial` when `PageError` is set and `CollectedItems > 0` (evaluation ran)
- [x] 3.2 When `PageError` is set and `CollectedItems == 0`, return `Failed` with `unmatched=0` (not `unmatched=collected`)

## 4. FeedCrawlManager — handle CompletedPartial

- [x] 4.1 Add `CrawlStatus.CompletedPartial` case in `HandleResult`: persist `FeedCursorAdvanced` with `gap=true`, reset consecutive failures

## 5. FeedOptions — MVW query optimization

- [x] 5.1 Change `FeedOptions` defaults: `PageSize=1000`, `MaxPages=20`, `PagePrefetch=2`

## 6. Tests

- [x] 6.1 Test `GetCrawlResult` returns `CompletedPartial` when `PageError` set and items collected
- [x] 6.2 Test `GetCrawlResult` returns `Failed` with `unmatched=0` when `PageError` set and 0 items
- [x] 6.3 Test `FeedCrawlManager.HandleResult` advances cursor on `CompletedPartial` with items
- [x] 6.4 Test `HandlePageResult` calls `StartEvaluating` regardless of which page arrives last (verify the race condition is gone)

## 7. Verify

- [x] 7.1 Build and deploy to dev stack, trigger feed crawl with MVW stable — verify published > 0
- [x] 7.2 Trigger feed crawl with simulated partial failure (docker network disconnect mid-harvest) — verify items still evaluated and published
