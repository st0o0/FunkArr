## 1. Response models

- [x] 1.1 Add `OperationResult` record to `Models/` — `sealed record OperationResult(bool Success, string? Error)`
- [x] 1.2 Add `ErrorResponse` and `ValidationErrorResponse` records to `Models/`
- [x] 1.3 Add `TestScoreResponse` record to `Models/` — `sealed record TestScoreResponse(ItemTrace[] ItemTraces)`
- [x] 1.4 Add `CreatedRuleSetResponse` record to `Models/`
- [x] 1.5 Move `MediathekSearchResponse` and `MediathekSearchResult` from `MediathekApiEndpoints.cs` to `Models/MediathekSearch.cs`

## 2. Request models

- [x] 2.1 Add `CreateRuleSetRequest` record to `Models/` with `RuleSetId`, `Topic`, and remaining ruleset body as `JsonElement` (for pass-through to validator)
- [x] 2.2 Add `UpdateRuleSetRequest` record to `Models/` with ruleset body as `JsonElement`
- [x] 2.3 Add `TestScoreRequest` record to `Models/` with `Config` (`TestRuleConfig`) and `Candidates` (`TestCandidate[]`)
- [x] 2.4 Add `TestRuleConfig`, `TestRule`, `TestCandidate`, and `TestTitleRule` records to `Models/`
- [x] 2.5 ~~Add `FilterNodeJsonConverter`~~ — replaced by flat `TestFilterNode` model; mapper handles type discrimination
- [x] 2.6 Add static `TestRuleMapper` helper to map `TestRule` to domain `MatchingRule` (strategy string → `IdentificationSpec`)

## 3. Wire up endpoints

- [x] 3.1 Update `POST /api/rulesets` (HandleCreate) to accept `CreateRuleSetRequest` instead of `JsonElement`, use `ErrorResponse`/`ValidationErrorResponse`/`CreatedRuleSetResponse`
- [x] 3.2 Update `PUT /api/rulesets/{id}` (HandleUpdate) to accept `UpdateRuleSetRequest` instead of `JsonElement`, use `ValidationErrorResponse`
- [x] 3.3 Update `POST /api/rulesets/test` to accept `TestScoreRequest`, use `TestRuleMapper` for conversion, return `TestScoreResponse`
- [x] 3.4 Delete `RuleSetTestRequestParser.cs`
- [x] 3.5 Update `DELETE /api/downloads/queue/{id}`, `DELETE /api/downloads/history/{id}`, `POST /api/downloads/{id}/retry` to return `OperationResult`
- [x] 3.6 Update `MediathekApiEndpoints.cs` to reference models from `Models/`, remove inline records
- [x] 3.7 Update all `.Produces<>()` declarations to reference actual typed models (remove `Produces<object>()`)

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` — no errors
- [x] 4.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 4.3 Run API tests: `dotnet run --project src/FunkArr.Api.Tests/FunkArr.Api.Tests.csproj`
