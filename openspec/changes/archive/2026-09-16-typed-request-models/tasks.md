## 1. Shared Input Models

- [x] 1.1 Create `Models/RuleSetBody.cs` with shared input records: `MediaInput`, `RuleInput`, `FilterGroupInput`, `FilterNodeInput`, `TitleRuleInput` — using Messages enums (`IdentificationStrategy`, `FilterField`, `FilterOp`, `TitlePartType`)
- [x] 1.2 Delete old duplicate types from `Models/TestScoreRequest.cs`: remove `TestRuleConfig`, `TestRule`, `TestFilterSpec`, `TestFilterNode`, `TestTitleRule` — keep only `TestScoreRequest` and `TestCandidate`

## 2. Request Models

- [x] 2.1 Rewrite `Models/CreateRuleSetRequest.cs` as typed record with `RuleSetId`, `Topic`, `Media` (MediaInput), `Rules` (RuleInput[]), `Aliases`, `Confidence`, `Standalone`, `Disable` — no JsonExtensionData
- [x] 2.2 Rewrite `Models/UpdateRuleSetRequest.cs` as typed record identical to CreateRuleSetRequest but without `RuleSetId`
- [x] 2.3 Rewrite `Models/TestScoreRequest.cs` to use `RuleInput[]` directly instead of `TestRule[]`, remove `TestRuleConfig` wrapper

## 3. Disk Serialization

- [x] 3.1 Create shared `JsonSerializerOptions` for disk writes (camelCase, WhenWritingNull, WriteIndented) — either as static field or helper method
- [x] 3.2 Update `HandleCreate` to serialize typed `CreateRuleSetRequest` to JSON for disk (excluding `ruleSetId` from the JSON body)
- [x] 3.3 Update `HandleUpdate` to serialize typed `UpdateRuleSetRequest` to JSON for disk

## 4. Endpoint Updates

- [x] 4.1 Update `HandleCreate` — remove manual `TryGetValue("topic", ...)` checks, use typed model, keep schema validation + ruleSetId regex check
- [x] 4.2 Update `HandleUpdate` — remove `JsonSerializer.Serialize(request.Body, ...)`, use typed model
- [x] 4.3 Update test scoring endpoint — use new `TestScoreRequest` with `RuleInput[]`

## 5. Mapping Extensions

- [x] 5.1 Simplify `TestScoreMappingExtensions.cs` — `RuleInput.ToMessage()` replaces `TestRule.ToMessage()`, mapping is simpler since enums are already typed
- [x] 5.2 Remove any unused mapping methods from old TestRule/TestFilterSpec types

## 6. Tests & Verification

- [x] 6.1 Update API endpoint tests for new request model types
- [x] 6.2 Run `dotnet build src/FunkArr.slnx` and fix all compilation errors
- [x] 6.3 Run all test projects and verify passing
- [x] 6.4 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
