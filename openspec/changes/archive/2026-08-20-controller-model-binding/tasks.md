## 1. Global JSON Configuration

- [x] 1.1 Configure `AddControllers().AddJsonOptions()` in `FunkArrServiceSetup` with camelCase, WhenWritingNull, and JsonStringEnumConverter
- [x] 1.2 Remove `SetupValidationJsonOptions` class (options match global config) and update `SetupValidationModels.cs`

## 2. Response Models

- [x] 2.1 Add `DeletedResponse`, `ReloadedResponse` records to `Api/Models/`
- [x] 2.2 Move `TestRulesRequest` from private nested record in RulesetController to `Api/Models/TestRulesRequest.cs` as public record
- [x] 2.3 Add `TestRulesResponse` record to `Api/Models/`
- [x] 2.4 Add SABnzbd typed response records to `Api/Models/SabnzbdResponses.cs`: `SabnzbdVersionResponse`, `SabnzbdConfigResponse`, `SabnzbdQueueResponse`, `SabnzbdHistoryResponse`, `SabnzbdAddFileResponse`, `SabnzbdErrorResponse` — with `[JsonPropertyName]` for snake_case properties

## 3. SetupController

- [x] 3.1 Add `[FromBody]` to `TestProwlarr`, `TestArr`, `TestPaths` actions — remove manual null checks that model binding handles
- [x] 3.2 Change `Validate` to use `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ValidationRequest? request` — remove manual `Request.ReadFromJsonAsync` and `Request.ContentLength` check
- [x] 3.3 Replace `new JsonResult(result, SetupValidationJsonOptions.Default)` with `Ok(result)` in Validate

## 4. RulesetController

- [x] 4.1 Replace manual `JsonSerializer.DeserializeAsync(Request.Body)` in `Save()` with `[FromBody] RuleSetFile ruleSet` parameter
- [x] 4.2 Replace manual `JsonSerializer.DeserializeAsync(Request.Body)` in `Test()` with `[FromBody] TestRulesRequest request` parameter
- [x] 4.3 Replace `new JsonResult(response.RuleSet, RuleSetJsonOptions.Default)` in `GetOne()` with `Ok(response.RuleSet)`
- [x] 4.4 Replace anonymous objects in `Delete()` and `Reload()` with typed `DeletedResponse` / `ReloadedResponse`
- [x] 4.5 Replace anonymous object in `Test()` response with typed `TestRulesResponse`
- [x] 4.6 Add `[ProducesResponseType]` attributes to all actions

## 5. NewznabController

- [x] 5.1 Add `[FromQuery]` parameters to `HandleNewznabRequest`: `string? t`, `string? q`, `int? tvdbid`, `int? season`, `[FromQuery(Name = "ep")] int? episode`, `string? imdbid`
- [x] 5.2 Add `[FromQuery]` parameters to `HandleFakeNzbDownload`: `string? url`, `string? title`, `string? subtitle`
- [x] 5.3 Remove manual `Request.Query` reads from all handler methods — pass bound parameters through
- [x] 5.4 Route RSS feed (empty query) through `SearchActor.TextSearchRequest("")` instead of direct MediathekClient call — remove `HandleRssFeed` method

## 6. SabnzbdController

- [x] 6.1 Add `[FromQuery] string? mode` to GET handler — remove manual `Request.Query["mode"]` read
- [x] 6.2 Replace `Request.ReadFormAsync()` / `form.Files.FirstOrDefault()` in POST handler with `[FromForm] IFormFile? file` parameter
- [x] 6.3 Replace all anonymous response objects with typed SABnzbd response records
- [x] 6.4 Add `[ProducesResponseType]` attributes to GET and POST actions

## 7. MatchIntelligenceController

- [x] 7.1 Add missing `[ProducesResponseType]` attributes with typed response types to all actions

## 8. Cleanup & Verification

- [x] 8.1 Remove unused `using` statements for `System.Text.Json` in controllers that no longer manually deserialize
- [x] 8.2 Run `dotnet format` on all changed files
- [x] 8.3 Build solution and fix any compilation errors
- [x] 8.4 Run all tests and fix any failures
