## 1. Configuration

- [x] 1.1 Add `RssFeedOptions` record to `FunkArr.Configuration` with `RefreshIntervalMinutes` (default 30) and `MaxItems` (default 500)
- [x] 1.2 Register `RssFeedOptions` in `FunkArrServiceSetup` via options binding

## 2. RssFeedCoordinator Actor

- [x] 2.1 Create `RssFeedCoordinator` actor in `FunkArr.Indexer` namespace with message types: `RefreshFeed`, `GetRssFeed(int Limit, int Offset)`, `RssFeedResponse(IReadOnlyList<SearchResult> Items, int Total)`
- [x] 2.2 Implement timer-based scheduling: immediate refresh on startup, periodic refresh on configured interval using `IWithTimers`
- [x] 2.3 Implement refresh cycle: ask `RuleSetCoordinator.GetAllRulesets`, extract distinct topics, query `SearchCoordinator.TextSearchRequest` for each topic sequentially with 1s delay
- [x] 2.4 Implement result aggregation: deduplicate by URL, sort by timestamp descending, bound to `MaxItems`
- [x] 2.5 Implement `GetRssFeed` handler: return paginated slice of cached results
- [x] 2.6 Implement refresh resilience: catch per-topic failures, retain previous cache on complete failure

## 3. Actor Registration

- [x] 3.1 Register `RssFeedCoordinator` as singleton in `FunkArrActorSystemSetup` with DI dependencies (`IOptions<RssFeedOptions>`)

## 4. NewznabController Integration

- [x] 4.1 Add `limit` and `offset` query parameters to `HandleNewznabRequest` method
- [x] 4.2 Modify `HandleRssFeed` to ask `RssFeedCoordinator.GetRssFeed` instead of `SearchCoordinator.TextSearchRequest("")`
- [x] 4.3 Pass `limit` (default 100) and `offset` (default 0) from query params to `GetRssFeed`

## 5. Testing

- [x] 5.1 Write actor test for `RssFeedCoordinator`: verify refresh cycle queries all ruleset topics
- [x] 5.2 Write actor test for `RssFeedCoordinator`: verify pagination, deduplication, and bounded cache
- [x] 5.3 Write actor test for `RssFeedCoordinator`: verify resilience on topic query failure

## 6. Verification

- [ ] 6.1 Start dev stack, configure a ruleset, verify Prowlarr can add FunkArr as indexer (test passes)
- [ ] 6.2 Verify Sonarr RSS sync picks up content from FunkArr
