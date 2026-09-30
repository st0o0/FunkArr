## Why

The ArrApi project has zero DI participation — `SearchResultCache` and `SearchHandler` are manually instantiated inline, `ApiKeyEndpointFilter` does service location, and all business logic lives in two god-files (`NewznabApiEndpoints` 168 LOC, `SabnzbdApiEndpoints` 312 LOC) that mix routing, serialization, formatting, and protocol translation. There is also a confirmed double-paging bug in `SearchHandler`. Refactoring to controllers with proper DI and extracted services makes the code testable, maintainable, and true to its mandate as a thin translation layer.

## What Changes

- **BREAKING**: Replace `MapNewznabApi()` / `MapSabnzbdApi()` static extension methods with `NewznabController` and `SabnzbdController`
- **BREAKING**: Add `AddControllers()` / `MapControllers()` to startup, replace Minimal API endpoint registration
- Extract `NewznabSearchService` (search → actor Ask → RSS building) from `SearchHandler` + `NewznabApiEndpoints`
- Extract `NzbService` (NZB get/parse/generate) from inline `NewznabApiEndpoints` logic, move `Nzb.cs` into `Newznab/`
- Extract `SabnzbdQueueService` (queue/history/fullstatus → actor Ask → JSON mapping) from `SabnzbdApiEndpoints`
- Extract `SabnzbdDownloadService` (add/delete/retry → actor commands) from `SabnzbdApiEndpoints`
- Extract `SabnzbdResponseMapper` (QueueSlot/HistorySlot building, format helpers) from `SabnzbdApiEndpoints`
- Create `AddArrApiServices()` IServiceCollection extension for proper DI registration
- Create `ArrApiOptions` for configurable timeouts and cache TTL (replacing hardcoded 30s/10s/60s values)
- Convert `ApiKeyEndpointFilter` to `IActionFilter` / `IAsyncActionFilter` with constructor DI
- Create `NewznabXmlResult` custom `IActionResult` for XML content responses
- Fix double-paging bug: remove `Skip/Take` from `ToRss`, paging only in the service layer
- Fix `NewznabCategory.FromCat` to explicitly handle TV range (5000-5999)
- Clean up `SearchResultCache`: remove redundant catch `TryRemove`, keep only `finally`
- Delete `NewznabApiEndpoints.cs` and `SabnzbdApiEndpoints.cs` (replaced by controllers + services)
- Update all existing tests for new service class structure

## Capabilities

### New Capabilities

_None — this is a structural refactor of existing capabilities._

### Modified Capabilities

- `arr-api-structure`: Controllers replace Minimal API endpoints; `AddArrApiServices()` replaces zero-DI instantiation; `ApiKeyEndpointFilter` becomes `IActionFilter`; `Nzb.cs` moves into `Newznab/` namespace
- `newznab-indexer-api`: DI resolution changes from closure-captured to constructor injection; `NewznabApiEndpoints` replaced by `NewznabController` + services; double-paging bug fixed
- `sabnzbd-download-api`: Static methods replaced by injectable services; `SabnzbdApiEndpoints` replaced by `SabnzbdController` + services

## Impact

- **FunkArr.ArrApi**: Major restructure — new files, deleted files, moved files
- **FunkArr (host)**: `DownloadSetupContainer` changes from `app.MapNewznabApi()` / `app.MapSabnzbdApi()` to `services.AddArrApiServices()` + `app.MapControllers()`
- **FunkArr.ArrApi.Tests**: Tests need updating for new service class structure (constructor injection instead of static methods)
- **No impact**: Messages, actors, domain projects, frontend, other API surfaces
