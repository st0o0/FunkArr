## 1. Infrastructure & Options

- [x] 1.1 Create `ArrApiOptions` configuration class with `SearchTimeoutSeconds` (30), `DownloadTimeoutSeconds` (10), `SearchCacheTtlSeconds` (60)
- [x] 1.2 Create `ServiceCollectionExtensions.AddArrApiServices()` registering options binding, `SearchResultCache`, `NewznabSearchService`, `NzbService`, `SabnzbdQueueService`, `SabnzbdDownloadService`
- [x] 1.3 Create `NewznabXmlResult : IActionResult` for XML content responses with Newznab namespaces
- [x] 1.4 Convert `ApiKeyEndpointFilter` to `ApiKeyActionFilter` base class with `IOptions<FunkArrOptions>` constructor injection, plus `NewznabApiKeyFilter` and `SabnzbdApiKeyFilter` subclasses

## 2. Bug Fixes & Cleanup

- [x] 2.1 Fix `NewznabCategory.FromCat` to handle TV range (>= 5000 and < 6000 => Tv)
- [x] 2.2 Fix `SearchResultCache.GetOrAddAsync` — remove redundant catch `TryRemove`, keep only `finally`

## 3. Newznab Services

- [x] 3.1 Extract `NewznabSearchService` from `SearchHandler` + `NewznabApiEndpoints` — constructor-injected `IActorRegistry`, `SearchResultCache`, `IOptions<ArrApiOptions>`, `ILogger`. Fix double-paging: paging in service only, RSS mapper receives pre-paged items
- [x] 3.2 Extract `NzbService` — NZB get (decode base64 GUID → build NZB XML) and NZB parse (XML → metadata). Move `Nzb.cs` from root to `Newznab/` namespace
- [x] 3.3 Move XML serialization helpers (`Serialize`, `Utf8StringWriter`, `XmlResult`) into `NewznabXmlResult` or a shared `NewznabXmlSerializer` utility

## 4. SABnzbd Services

- [x] 4.1 Extract `SabnzbdResponseMapper` — static helper with `BuildQueueSlot`, `BuildHistorySlot`, `FormatSpeed`, `FormatTimeLeft`, `MapMediaTypeToCategory`, `ParseMediaType`
- [x] 4.2 Extract `SabnzbdQueueService` — constructor-injected `IActorRegistry`, `IOptions<ArrApiOptions>`, `IOptions<DownloadOptions>`, `DataPaths`. Methods: `GetQueue()`, `GetHistory()`, `GetFullStatus()`, `GetConfig()`
- [x] 4.3 Extract `SabnzbdDownloadService` — constructor-injected `IActorRegistry`, `NzbService`, `IOptions<ArrApiOptions>`, `ILogger`. Methods: `AddFile()`, `DeleteFromQueue()`, `DeleteFromHistory()`, `Retry()`

## 5. Controllers

- [x] 5.1 Create `NewznabController` — `[ApiController]`, `[Route("/index/api")]`, `[ServiceFilter(typeof(NewznabApiKeyFilter))]`. Single GET action dispatching on `t` parameter to `NewznabSearchService`, `NzbService`, or inline caps
- [x] 5.2 Create `SabnzbdController` — `[ApiController]`, `[Route("/download/api")]`, `[ServiceFilter(typeof(SabnzbdApiKeyFilter))]`. GET action dispatching on `mode` parameter, POST action for `addfile`

## 6. Host Wiring

- [x] 6.1 Update `DownloadSetupContainer` — replace `app.MapNewznabApi()` / `app.MapSabnzbdApi()` with `services.AddArrApiServices(configuration)` in `SetupServices` and ensure `app.MapControllers()` is called (or add `AddControllers()` if not already present)
- [x] 6.2 Delete `NewznabApiEndpoints.cs`, `SabnzbdApiEndpoints.cs`, `SearchHandler.cs`, old `ApiKeyEndpointFilter.cs`. `Nzb.cs` kept in place with namespace changed to `FunkArr.ArrApi.Newznab`

## 7. Tests

- [x] 7.1 Update existing serialization/model tests for namespace changes (Nzb moved to Newznab/)
- [x] 7.2 SearchResultCache constructor kept as (TimeSpan, TimeProvider) — DI registration extracts from options. No test changes needed.
- [x] 7.3 Updated SearchResultMappingTests for NewznabSearchService.ToRss static method, single-pass paging, ParseInt, BuildAttributes
- [x] 7.4 Add tests for `NzbService` — get and parse operations (NzbServiceTests: 8 tests)
- [x] 7.5 Add tests for `SabnzbdResponseMapper` — slot building, format helpers (SabnzbdResponseMapperTests: 11 tests)
- [x] 7.6 Add tests for `ApiKeyActionFilter` subclasses (ApiKeyActionFilterTests: 5 tests)
- [x] 7.7 Run all test projects, fix any regressions — 673 tests across 10 projects all pass

## 8. Validation

- [x] 8.1 `dotnet build src/FunkArr.slnx` compiles clean (0 errors, 0 warnings)
- [x] 8.2 `dotnet format src/FunkArr.slnx --verify-no-changes` passes (only pre-existing IDE1006 in DownloadScheduler.cs)
- [x] 8.3 All test projects pass — 91 ArrApi tests (up from 64), 673 total
- [x] 8.4 Architecture tests pass (12/12)
