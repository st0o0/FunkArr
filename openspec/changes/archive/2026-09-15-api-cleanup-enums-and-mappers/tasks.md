## 1. IdentificationStrategy Expansion

- [x] 1.1 Expand `IdentificationStrategy` enum to 5 members (SeasonAndEpisodeNumber, AbsoluteEpisodeNumber, TitleExact, TitleIncludes, AirdateExtraction) with `[JsonPropertyName]` attributes and `[JsonConverter(typeof(JsonStringEnumConverter<IdentificationStrategy>))]`
- [x] 1.2 Delete `TitleMatchMode.cs` enum
- [x] 1.3 Update `IdentificationSpec` record to remove `MatchMode` parameter
- [x] 1.4 Update `RuleSetMerger.TransformIdentification` to create `IdentificationSpec` with the 5-member enum directly (no more MatchMode)
- [x] 1.5 Update MatchMagic evaluation to switch on 5-member strategy enum without TitleMatchMode

## 2. Messages Enums — JsonStringEnumConverter

- [x] 2.1 Add `[JsonConverter(typeof(JsonStringEnumConverter<FilterField>))]` to `FilterField` enum
- [x] 2.2 Add `[JsonConverter(typeof(JsonStringEnumConverter<FilterOp>))]` to `FilterOp` enum
- [x] 2.3 Add `[JsonConverter(typeof(JsonStringEnumConverter<TitlePartType>))]` to `TitlePartType` enum
- [x] 2.4 Update `RuleSetMerger` internal classes: `RawRule.Strategy` → `IdentificationStrategy?`, `RawFilterGroup` conditions use `FilterField`/`FilterOp`, `RawTitleRule.Type` → `TitlePartType?`
- [x] 2.5 Remove `ParseFilterField`, `ParseFilterOp`, and `ParseFilterField(string?)` switch methods from `RuleSetMerger`

## 3. API Enum Types

- [x] 3.1 Create `QueueStatus` enum in Api.Models (Processing=0, Queued=1), update `DownloadQueueItem.Status` from string to `QueueStatus`
- [x] 3.2 Create `HistoryStatus` enum in Api.Models (Completed=0, Failed=1), update `DownloadHistoryItem.Status` from string to `HistoryStatus`
- [x] 3.3 Create `SourceType` enum in Api.Models (Community=0, Local=1, Merged=2), update `RuleSetListEntry.SourceType` from string to `SourceType`
- [x] 3.4 Create `MediaType` enum in Api.Models (Show=0, Movie=1), update `RuleSetListEntry.MediaType` from string to `MediaType`
- [x] 3.5 Create `CheckStatus` enum in Api.Models (Ok=0, Warn=1, Fail=2), update `CheckResult.Status` from string to `CheckStatus`, update factory methods

## 4. Date Type Fixes

- [x] 4.1 Change `DownloadHistoryItem.CompletedAt` from string to `DateTimeOffset`
- [x] 4.2 Change `RuleSetListEntry.LastScoringRun` from `string?` to `DateTimeOffset?`
- [x] 4.3 Remove `.ToString("o")` conversions in endpoint mapper code

## 5. ToApi() Extension Methods

- [x] 5.1 Create `Extensions/RuleSetMappingExtensions.cs` with `RuleSetDetailResult.ToApi()`, `ScoringHistoryResult.ToApi()`, `ScoringDetailResult.ToApi()` and remove corresponding private methods from `RuleSetApiEndpoints.cs`
- [x] 5.2 Create `Extensions/ScoringMappingExtensions.cs` with `ItemTrace.ToApi()`, `RuleTrace.ToApi()`, `FilterGroupTrace.ToApi()`, `RuleOutcome.ToApi()` and remove from `RuleSetApiEndpoints.cs`
- [x] 5.3 Create `Extensions/DownloadMappingExtensions.cs` with `QueueResult.ToApi()`, `QueueItem.ToApi()`, `HistoryResult.ToApi()`, `HistoryItem.ToApi()` and remove from `DownloadsApiEndpoints.cs`
- [x] 5.4 Create `Extensions/MediathekMappingExtensions.cs` with `MediathekItem.ToApi()` and `EstimateQuality` and remove from `MediathekApiEndpoints.cs`
- [x] 5.5 Create `Extensions/TestScoreMappingExtensions.cs` with `TestScoreRequest.ToMessage()`, `TestRule.ToMessage()` and delete `TestRuleMapper.cs`

## 6. Endpoint Cleanup

- [x] 6.1 Update `RuleSetApiEndpoints.cs` to use `.ToApi()` / `.ToMessage()` extensions, remove all private mapper methods
- [x] 6.2 Update `DownloadsApiEndpoints.cs` to use `.ToApi()` extensions, switch `registry.Get<>()` to `registry.GetAsync<>()`, remove mapper methods
- [x] 6.3 Update `MediathekApiEndpoints.cs` to use `.ToApi()` extensions, remove mapper methods
- [x] 6.4 Extract shared `ApiResults.GatewayTimeout()` helper, replace all 3 duplicate methods

## 7. Request Model Cleanup

- [x] 7.1 Update `TestRule.Strategy` from `string?` to `IdentificationStrategy?`
- [x] 7.2 Update `TestFilterNode.Field` from `string?` to `FilterField?` and `Op` from `string?` to `FilterOp?`
- [x] 7.3 Update `TestTitleRule.Type` from `string` to `TitlePartType`

## 8. Stale File Cleanup

- [x] 8.1 Delete `openapi/funkArr-v1.yaml`
- [x] 8.2 Delete `openapi/` directory if empty after removal

## 9. Tests & Verification

- [x] 9.1 Update `RuleSetMergerTests` for enum-typed strategy/field/op
- [x] 9.2 Update API endpoint tests for int-serialized enums and new extension methods
- [x] 9.3 Run `dotnet build src/FunkArr.slnx` and fix all compilation errors
- [x] 9.4 Run all test projects and verify passing
- [x] 9.5 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
