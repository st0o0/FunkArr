## 1. RuleSet endpoint consolidation

- [x] 1.1 Create `RuleSetTestRequestParser.cs` in FunkArr.Api — extract all JSON parsing methods (`ParseTestRequest`, `ParseRules`, `ParseIdentification`, `ParseTitleRules`, `ParseFilterSpec`, `ParseFilterNodes`, `ParseFilterCondition`, `ParseFilterField`, `ParseFilterOp`, `ParseCandidates`) from `RuleSetTestApiEndpoints.cs` into a static `RuleSetTestRequestParser` class with a public `Parse()` method
- [x] 1.2 Merge `RuleSetWriteApiEndpoints` and `RuleSetTestApiEndpoints` routes into `RuleSetApiEndpoints.cs` — single `MapRuleSetApi()` method, single `MapGroup("/api/rulesets").WithTags("Rulesets")` call. Move write handler methods and the `GeneratedRegex` into the merged file. Test endpoint delegates parsing to `RuleSetTestRequestParser.Parse()`
- [x] 1.3 Delete `RuleSetWriteApiEndpoints.cs` and `RuleSetTestApiEndpoints.cs`
- [x] 1.4 Update `RuleSetSetupContainer` — replace three `Map*` calls with single `app.MapRuleSetApi()`
- [x] 1.5 Update test files `RuleSetWriteApiEndpointTests.cs` and `RuleSetTestApiEndpointTests.cs` to call `MapRuleSetApi()` instead of the old methods

## 2. Rename QueueApiEndpoints → DownloadsApiEndpoints

- [x] 2.1 Rename `QueueApiEndpoints.cs` → `DownloadsApiEndpoints.cs`, class `QueueApiEndpoints` → `DownloadsApiEndpoints`, method `MapQueueApi()` → `MapDownloadsApi()`
- [x] 2.2 Update `DownloadSetupContainer` — `app.MapQueueApi()` → `app.MapDownloadsApi()`

## 3. Rename SetupApiEndpoints → SystemApiEndpoints

- [x] 3.1 Rename `SetupApiEndpoints.cs` → `SystemApiEndpoints.cs`, class `SetupApiEndpoints` → `SystemApiEndpoints`, method `MapSetupApi()` → `MapSystemApi()`, route prefix `/api/health` → `/api/system`, tag `"Health"` → `"System"`
- [x] 3.2 Update `ApplicationSetupContainer` — `app.MapSetupApi()` → `app.MapSystemApi()`
- [x] 3.3 Update `src/FunkArr.UI/src/api/setup.ts` — change `/api/health/*` paths to `/api/system/*`

## 4. Rename ArrApi endpoint files

- [x] 4.1 Rename `Newznab/IndexerApiEndpoints.cs` → `Newznab/NewznabApiEndpoints.cs`, class `IndexerApiEndpoints` → `NewznabApiEndpoints`, method `MapIndexerApi()` → `MapNewznabApi()`, tag `"Indexer (Newznab)"` → `"Newznab"`
- [x] 4.2 Rename `Sabnzbd/DownloadApiEndpoints.cs` → `Sabnzbd/SabnzbdApiEndpoints.cs`, class `DownloadApiEndpoints` → `SabnzbdApiEndpoints`, method `MapDownloadApi()` → `MapSabnzbdApi()`, tag `"Download Client (SABnzbd)"` → `"SABnzbd"`
- [x] 4.3 Update `DownloadSetupContainer` — `app.MapDownloadApi()` → `app.MapSabnzbdApi()`
- [x] 4.4 Update any test files or references that use old ArrApi class/method names

## 5. Verify

- [x] 5.1 Run `dotnet build src/FunkArr.slnx` — ensure clean build
- [x] 5.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.3 Run all test projects that reference endpoint classes
