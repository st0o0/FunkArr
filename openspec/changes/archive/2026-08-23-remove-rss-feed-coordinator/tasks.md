## 1. Remove RssFeedCoordinator Infrastructure

- [x] 1.1 Delete `RssFeedCoordinator.cs` from `FunkArr.Indexer`
- [x] 1.2 Delete `RssFeedOptions.cs` from `FunkArr.Configuration`
- [x] 1.3 Remove `RssFeedCoordinator` singleton registration from `FunkArrActorSystemSetup`
- [x] 1.4 Remove `RssFeedOptions` binding from `FunkArrServiceSetup`
- [x] 1.5 Remove `RssFeed` section from `appsettings.json` and `appsettings.Development.json` (if present)

## 2. Simplify NewznabController

- [x] 2.1 Remove `HandleRssFeed` method from `NewznabController`
- [x] 2.2 Modify `HandleTvSearch` to send `TextSearchRequest("")` through `SearchRouter` when no `tvdbid`/`q` provided (instead of branching to RSS)
- [x] 2.3 Modify `HandleTextSearch` to send `TextSearchRequest(q ?? "")` through `SearchRouter` (remove empty-query branch)
- [x] 2.4 Apply `limit`/`offset` pagination on search results in the controller before converting to Newznab XML
- [x] 2.5 Remove `RssFeedCoordinator` actor resolution from the controller (controller should only resolve `SearchRouter`)

## 3. Update Tests

- [x] 3.1 Delete `RssFeedCoordinatorTests.cs`
- [x] 3.2 Verify existing `TextSearchPipeline` tests cover empty-query behavior (add test if missing)
- [x] 3.3 Run full test suite and fix any compilation errors from removed types

## 4. Build Verification

- [x] 4.1 Run `dotnet build` and confirm zero errors
- [x] 4.2 Run `dotnet run --project FunkArr.Tests` and confirm all tests pass
- [x] 4.3 Run `dotnet format` to ensure formatting compliance
