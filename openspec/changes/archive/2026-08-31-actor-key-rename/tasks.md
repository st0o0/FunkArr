## 1. Actor Keys and Registration

- [x] 1.1 Rename interfaces in `FunkArr.Core/ActorKeys.cs`: `IMediathekGateway` → `IMediathekManager`, `IRuleSetService` → `IRuleSetManager`, `IMatchMagicService` → `IMatchMagicManager`, `ISearchGateway` → `ISearchManager`, `IMatchHistoryService` → `IMatchHistoryRegion`
- [x] 1.2 Update `AkkaSetupContainer.cs` — key types and actor name string `"search-gateway-manager"` → `"search-manager"`

## 2. Actor Class Rename

- [x] 2.1 Rename `SearchGatewayManager` class to `SearchManager` and rename file `SearchGatewayManager.cs` → `SearchManager.cs`
- [x] 2.2 Update all references to `SearchGatewayManager` in production code

## 3. Consumer Updates

- [x] 3.1 Update `Context.GetActor<T>()` calls in `MovieSearchWorker.cs`, `TvSearchWorker.cs`, `RuleSetWorker.cs`, `RuleSetManager.cs`
- [x] 3.2 Update `registry.Register<T>()` calls in test files (`MovieSearchWorkerTests.cs`, `TvSearchWorkerTests.cs`, `SearchGatewayManagerTests.cs`)
- [x] 3.3 Rename `SearchGatewayManagerTests.cs` → `SearchManagerTests.cs` and update class name

## 4. Verification

- [x] 4.1 `dotnet build FunkArr.slnx` passes
- [x] 4.2 `dotnet format --verify-no-changes` passes
- [x] 4.3 Run all test projects
