## 1. Infrastructure & Dependencies

- [x] 1.1 Add NuGet packages to `Directory.Packages.props`: `Asp.Versioning.Mvc`, `Asp.Versioning.Mvc.ApiExplorer`, `Scalar.AspNetCore`
- [x] 1.2 Add package references to `FunkArr.csproj`: `Asp.Versioning.Mvc`, `Asp.Versioning.Mvc.ApiExplorer`, `Scalar.AspNetCore`
- [x] 1.3 Create folder structure: `src/FunkArr/Api/Controllers/` and `src/FunkArr/Api/Models/`
- [x] 1.4 Update `FunkArrServiceSetup`: add `services.AddControllers()`, API versioning configuration (`AddApiVersioning` with `UrlSegmentApiVersionReader`, `ReportApiVersions = true`, default version 1.0), and `services.AddOpenApi()`

## 2. Auth Middleware

- [x] 2.1 Create `ApiKeyMiddleware` in `src/FunkArr/Api/ApiKeyMiddleware.cs` with route-aware auth logic (skip public routes, XML error for bare `/api`, JSON 401 for everything else)
- [x] 2.2 Update `FunkArrApplicationSetup`: add `UseMiddleware<ApiKeyMiddleware>()` before `MapControllers()`

## 3. Shared Utilities & Response Models

- [x] 3.1 Create `PathMappingHelper` in `src/FunkArr/Api/PathMappingHelper.cs` extracting duplicated `ParsePathMapping`/`MapPath` from `QueueEndpoints` and `SabnzbdEndpoints`
- [x] 3.2 Create `ErrorResponse` record in `src/FunkArr/Api/Models/ErrorResponse.cs`
- [x] 3.3 Create queue response DTOs in `src/FunkArr/Api/Models/QueueResponses.cs`: `QueueItemResponse`, `HistoryItemResponse`
- [x] 3.4 Create setup response DTOs in `src/FunkArr/Api/Models/SetupResponses.cs`: `StatusResponse`, `TestConnectionResponse`, `TestPathsResponse`, `FfmpegResponse`, `ConfigResponse`
- [x] 3.5 Create match response DTOs in `src/FunkArr/Api/Models/MatchResponses.cs` (if needed beyond existing domain types)
- [x] 3.6 Create ruleset response DTOs in `src/FunkArr/Api/Models/RulesetResponses.cs`: `RulesetSummaryResponse`

## 4. Protocol Emulation Controllers (Unversioned)

- [x] 4.1 Create `NewznabController` in `src/FunkArr/Api/Controllers/NewznabController.cs` — migrate from `NewznabEndpoints.cs`, mark `[ApiVersionNeutral]`, tag `"Newznab Emulation"`, route `/api`
- [x] 4.2 Create `SabnzbdController` in `src/FunkArr/Api/Controllers/SabnzbdController.cs` — migrate from `SabnzbdEndpoints.cs`, mark `[ApiVersionNeutral]`, tag `"SABnzbd Emulation"`, route `/download/api`, remove inline `ValidateApiKey` (middleware handles it), use `PathMappingHelper`

## 5. Web UI Controllers (Versioned /api/v1/)

- [x] 5.1 Create `QueueController` in `src/FunkArr/Api/Controllers/QueueController.cs` — migrate from `QueueEndpoints.cs`, route `/api/v{version:apiVersion}`, use typed response DTOs, constructor-inject `ActorRegistry` and options, use `PathMappingHelper`
- [x] 5.2 Create `RulesetController` in `src/FunkArr/Api/Controllers/RulesetController.cs` — migrate from `RulesetEndpoints.cs`, route `/api/v{version:apiVersion}/rulesets`, constructor-inject `ActorRegistry`
- [x] 5.3 Create `MatchIntelligenceController` in `src/FunkArr/Api/Controllers/MatchIntelligenceController.cs` — migrate from `MatchIntelligenceEndpoints.cs`, route `/api/v{version:apiVersion}/matches`, use `[FromQuery]` for `limit` and `topic` parameters
- [x] 5.4 Create `SetupController` in `src/FunkArr/Api/Controllers/SetupController.cs` — migrate from `SetupEndpoints.cs`, combine `/api/v{version:apiVersion}/setup` and `/api/v{version:apiVersion}/config` routes, use typed response DTOs, move helper methods (`CheckFfmpeg`, `CheckMediathek`, `CheckProwlarr`, `CheckArrInstances`, `TestWriteAccess`, `MaskApiKey`) as private methods

## 6. Startup Wiring

- [x] 6.1 Update `FunkArrApplicationSetup.SetupApplication`: remove all 6 `Map*Endpoints()` calls, add `app.MapControllers()`, add `app.MapScalarApiReference()` and `app.MapOpenApi()`
- [x] 6.2 Verify pipeline order: `UseStaticFiles()` → `UseMiddleware<ApiKeyMiddleware>()` → `MapHealthChecks` → `MapGet("/alive")` → `MapControllers()` → `MapScalarApiReference()` → `MapOpenApi()` → `MapFallbackToFile`

## 7. Delete Old Endpoint Files

- [x] 7.1 Delete `src/FunkArr/Indexer/NewznabEndpoints.cs`
- [x] 7.2 Delete `src/FunkArr/Indexer/ApiKeyFilter.cs`
- [x] 7.3 Delete `src/FunkArr/DownloadClient/SabnzbdEndpoints.cs`
- [x] 7.4 Delete `src/FunkArr/DownloadClient/QueueEndpoints.cs` (includes `QueueApiKeyFilter`)
- [x] 7.5 Delete `src/FunkArr/RuleSet/RulesetEndpoints.cs`
- [x] 7.6 Delete `src/FunkArr/RuleSet/MatchIntelligenceEndpoints.cs` (includes `MatchApiKeyFilter`)
- [x] 7.7 Remove `SetupApiKeyFilter` class and endpoint methods from `src/FunkArr/Configuration/SetupEndpoints.cs`, then delete the file

## 8. Frontend Updates

- [x] 8.1 Add `API_BASE` constant to `src/FunkArr.UI/src/api/client.ts` and update `api()`, `apiPost()`, `apiPut()`, `apiDelete()` helpers to prepend it
- [x] 8.2 Update all Vue component API paths to use `API_BASE` prefix: `QueueView`, `HistoryView`, `RulesetsView`, `RulesetDetail`, `RulesetEditor`, `MatchesView`, `MatchTestPanel`, `SetupWizard`, `SettingsView`

## 9. Test Updates

- [x] 9.1 Update `EndpointTests.cs`: change all route assertions from `/api/*` to `/api/v1/*` for Web UI endpoints, verify Newznab/SABnzbd routes unchanged
- [x] 9.2 Update `WebUiEndpointTests.cs`: change all route assertions from `/api/*` to `/api/v1/*`
- [x] 9.3 Add middleware unit tests for `ApiKeyMiddleware`: test skip paths, XML error for Newznab, JSON error for versioned routes, pass-through for valid key
- [x] 9.4 Remove old auth filter tests if any exist (for `QueueApiKeyFilter`, `SetupApiKeyFilter`, `MatchApiKeyFilter`)

## 10. Verification

- [x] 10.1 Run `dotnet build FunkArr.slnx` — verify clean build with no warnings
- [x] 10.2 Run all tests via `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` — verify all pass
- [x] 10.3 Start the service (`dotnet run --project FunkArr/FunkArr.csproj`) and verify `/scalar` loads the API documentation UI
- [x] 10.4 Verify Newznab endpoint works at `/api?t=caps` (unchanged route)
- [x] 10.5 Verify a Web UI endpoint works at `/api/v1/queue` (new versioned route)
- [x] 10.6 Verify old route `/api/queue` returns 404
