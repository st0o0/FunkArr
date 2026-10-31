# Tasks

## 1. Policy

- [x] 1.1 Add `NewznabFeedCache` with the `NewznabFeed` policy (feed-request predicate, vary by `t`/`cat`/`offset`/`limit`/`apikey`, host and scheme, 60 s) and register it in `AddArrApiServices`. Verify with tests against a test server using the real policy (hit, misses per page/category/key, searches and other functions not cached).
- [x] 1.2 Apply `[OutputCache(PolicyName = "NewznabFeed")]` to the Newznab action. Verify with `dotnet build`.

## 2. Docs and verification

- [x] 2.1 Describe the cache in how-it-works (both languages).
- [ ] 2.2 Run `dotnet format --verify-no-changes` and all test projects via CI and verify they pass.
