# Tasks

## 1. Messages and wiring

- [x] 1.1 Add `FeedChanged` (Messages/Feed) and `IFeedCache` (Core). Verify with `dotnet build`.
- [x] 1.2 Register `FeedCacheActor` as `IFeedCache` in `AkkaSetupContainer` before the feed actors. Verify with `dotnet build`.

## 2. Cache

- [x] 2.1 Add `FeedResultCache` (generation-guarded store, clear) and register it as a singleton. Verify with unit tests (hit per key, clear, stale store after clear).
- [x] 2.2 Add `FeedCacheActor` clearing the cache on `FeedChanged`. Verify with an actor test.
- [x] 2.3 Read feed pages through the cache in `NewznabSearchService`; skip empty and failed results. Verify with integration tests (repeat served from cache, re-query after clear, wrong API key, empty feed not cached).

## 3. Invalidation

- [x] 3.1 Tell `FeedChanged` from `FeedManager` after persisted adds, persisted expiries and recovery. Verify by review and the integration path.

## 4. Docs and verification

- [x] 4.1 Describe the cache in how-it-works (both languages).
- [ ] 4.2 Run `dotnet format --verify-no-changes` and all test projects via CI and verify they pass.
