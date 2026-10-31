# Tasks

## 1. Policy

- [x] 1.1 Add `NewznabFeedCache` with the `NewznabFeed` policy (feed-request predicate, vary by `t`/`cat`/`offset`/`limit`/`apikey`, host and scheme, tag `feed`, 30 min, no-store marker) and register it in `AddArrApiServices`. Verify with tests against a test server using the real policy (hit, misses per page/category/key, searches and other functions not cached, marked responses not stored, eviction).
- [x] 1.2 Apply `[OutputCache(PolicyName = "NewznabFeed")]` to the Newznab action and mark validation fallbacks after a failed feed query as not storable. Verify with `dotnet build`.

## 2. Invalidation

- [x] 2.1 Add `IFeedCacheInvalidator` (Core) and `OutputCacheFeedInvalidator` (ArrApi, `EvictByTagAsync("feed")`). Verify with the eviction test.
- [x] 2.2 Call the invalidator from `FeedManager` after persisted adds, persisted expiries and recovery; log failures. Verify with FeedManager actor tests (recovery, new entries, known entries only, failing invalidator).

## 3. Docs and verification

- [x] 3.1 Describe the cache in how-it-works (both languages).
- [ ] 3.2 Run `dotnet format --verify-no-changes` and all test projects via CI and verify they pass.
