## 1. Extract RuleSetEnumMapping

- [x] 1.1 Create `RuleSetEnumMapping.cs` in `FunkArr.RuleSet` with bidirectional static methods for all enum types: `IdentificationStrategy`, `FilterField`, `FilterOp`, `TitlePartType`, `EnrichmentMethod`, `RuntimeMode`, `MediaType`
- [x] 1.2 Move `TryParseStrategy` logic from `RuleSetMerger` to `RuleSetEnumMapping.TryParseStrategy` and add reverse `ToDiskValue(this IdentificationStrategy)` method
- [x] 1.3 Add `ToDiskValue` / `TryParse` methods for `FilterField`, `FilterOp`, `TitlePartType`, `EnrichmentMethod`, `RuntimeMode`, `MediaType`

## 2. Rename RuleSetMerger to RuleSetReader

- [x] 2.1 Rename `RuleSetMerger.cs` → `RuleSetReader.cs` and class `RuleSetMerger` → `RuleSetReader`
- [x] 2.2 Update all references: `RuleSetManager`, `RuleSetManagerState`, `RuleSetWorker`, tests, `AkkaSetupContainer`
- [x] 2.3 Replace inline `TryParseStrategy` call in `RuleSetReader` with `RuleSetEnumMapping.TryParseStrategy`

## 3. Create RuleSetWriter service

- [x] 3.1 Create `RuleSetWriter.cs` in `FunkArr.RuleSet` — service class (not actor) with method `Serialize(CreateLocalRuleSet | UpdateLocalRuleSet) → string` that builds disk-format JSON using `RuleSetEnumMapping.ToDiskValue` for all enums
- [x] 3.2 Use `JsonSerializerOptions` with `CamelCase` naming, `WriteIndented`, `WhenWritingNull` ignore (match existing disk format) — no `JsonStringEnumConverter` needed since enums are mapped explicitly to strings before serialization

## 4. Create command/response messages

- [x] 4.1 Create `CreateLocalRuleSet` command in `FunkArr.Messages` with topic, aliases, media, confidence, rules, enrichment, standalone, disable fields using Messages-layer enum types
- [x] 4.2 Create `CreateLocalRuleSetResponse` hierarchy: `Completed(string RuleSetId)` / `Failed(FailureReason)` with `AlreadyExists`, `ValidationFailed(IReadOnlyList<RuleSetValidationError>)` reasons
- [x] 4.3 Create `UpdateLocalRuleSet` command with ruleSetId + same body fields
- [x] 4.4 Create `UpdateLocalRuleSetResponse` hierarchy: `Completed` / `Failed` with `NotFound`, `ValidationFailed` reasons
- [x] 4.5 Create `DeleteLocalRuleSet` command and `DeleteLocalRuleSetResponse`: `Completed` / `Failed(NotFound)`
- [x] 4.6 Create `ExportRuleSet` command and `ExportRuleSetResponse`: `Completed(string Json)` / `Failed(NotFound | ValidationFailed)`

## 5. Create LocalRuleSetWriter actor

- [x] 5.1 Create `LocalRuleSetWriter.cs` in `FunkArr.RuleSet` — `ReceiveActor` handling all four commands, injecting `IDataFiles`, `DataPaths`, `IRuleSetValidator`, `RuleSetWriter`
- [x] 5.2 Implement Create handler: check existing, serialize via `RuleSetWriter`, validate, create directory, write atomically, respond
- [x] 5.3 Implement Update handler: check exists (community or local), serialize, validate, write, respond
- [x] 5.4 Implement Delete handler: check local file exists, delete, respond
- [x] 5.5 Implement Export handler: read local (+ community if merged), flatten via `RuleSetReader`, validate, respond with JSON
- [x] 5.6 Register `LocalRuleSetWriter` as singleton in `AkkaSetupContainer` with `ILocalRuleSetWriter` key

## 6. Create API→Message mapping

- [x] 6.1 Add mapping extension methods in `FunkArr.Api/Extensions` to convert `CreateRuleSetRequest` → `CreateLocalRuleSet`, `UpdateRuleSetRequest` → `UpdateLocalRuleSet` (convert API enum numbers to Messages enum values)
- [x] 6.2 Include enrichment config mapping (extend existing `ToMessage()` pattern)

## 7. Refactor API endpoints

- [x] 7.1 Change `HandleCreate` to Ask `ILocalRuleSetWriter` with `CreateLocalRuleSet`, map response to HTTP status codes (201/409/422)
- [x] 7.2 Change `HandleUpdate` to Ask `ILocalRuleSetWriter` with `UpdateLocalRuleSet`, map response (200/404/422)
- [x] 7.3 Change `HandleDelete` to Ask `ILocalRuleSetWriter` with `DeleteLocalRuleSet`, map response (200/404)
- [x] 7.4 Change `HandleExport` to Ask `ILocalRuleSetWriter` with `ExportRuleSet`, map response (200/404/422)
- [x] 7.5 Remove `_diskJsonOptions`, `SerializeForDisk` methods, direct `IDataFiles`/`IRuleSetValidator` injections from `RuleSetApiEndpoints`
- [x] 7.6 Remove `JsonStringEnumMemberName` attributes from `FunkArr.Api.Models.Enums.cs` and `using System.Text.Json.Serialization` import

## 8. Tests

- [x] 8.1 Add unit tests for `RuleSetEnumMapping` — all enum types, both directions, unknown values
- [x] 8.2 Add unit tests for `RuleSetWriter.Serialize` — verify disk JSON output matches schema format (string enums, camelCase)
- [x] 8.3 Add `LocalRuleSetWriter` actor tests using TestKit — create, update, delete, export with success and failure scenarios
- [x] 8.4 Update existing `RuleSetMerger` tests to reference `RuleSetReader`
- [x] 8.5 Verify `dotnet build` + `dotnet format --verify-no-changes` pass
- [x] 8.6 Run all test projects to verify no regressions (715 tests, 0 failures)
