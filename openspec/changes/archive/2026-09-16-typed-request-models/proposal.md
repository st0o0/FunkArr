## Why

The Create/Update RuleSet endpoints use `[JsonExtensionData] Dictionary<string, JsonElement>` — completely opaque models with no OpenAPI schema and manual `TryGetValue` checks. Meanwhile, the TestScore endpoint already has typed models (`TestRule`, `TestFilterSpec`, etc.) that duplicate the same RuleSet structure. With `JsonStringEnumConverter` on Messages enums, request models can use domain enums directly, eliminating string mapping entirely.

## What Changes

- Create shared input records (`RuleSetBody`, `RuleInput`, `MediaInput`, `FilterGroupInput`, `FilterNodeInput`, `TitleRuleInput`) used by both RuleSet CRUD and TestScore endpoints
- Replace `CreateRuleSetRequest` and `UpdateRuleSetRequest` with typed models (no more JsonExtensionData)
- Rewrite `TestScoreRequest` to reuse shared `RuleInput` instead of separate `TestRule`/`TestFilterSpec`/`TestTitleRule` types
- Delete old duplicate types: `TestRule`, `TestRuleConfig`, `TestFilterSpec`, `TestFilterNode`, `TestTitleRule`
- Simplify `TestScoreMappingExtensions` — shared types reduce mapping code
- Serialize typed models back to JSON for disk write with `DefaultIgnoreCondition = WhenWritingNull`
- Remove manual `request.Body.TryGetValue("topic", ...)` checks — model binding handles required fields
- **BREAKING**: Request model shape changes (but frontend already sends matching structure)

## Capabilities

### New Capabilities
- `shared-input-models`: Shared typed input records for RuleSet data used across Create, Update, and TestScore endpoints

### Modified Capabilities
- `api-request-models`: CreateRuleSetRequest and UpdateRuleSetRequest use typed models instead of JsonExtensionData
- `api-mapping-extensions`: TestScoreMappingExtensions simplified to use shared input types

## Impact

- **FunkArr.Api/Models**: New shared input types, CreateRuleSetRequest/UpdateRuleSetRequest rewritten, TestScoreRequest simplified
- **FunkArr.Api/Extensions**: TestScoreMappingExtensions simplified
- **FunkArr.Api/RuleSetApiEndpoints.cs**: HandleCreate/HandleUpdate use typed models, test endpoint uses new TestScoreRequest
- **FunkArr.Api.Tests**: Tests updated for new request model types
- **OpenAPI/Scalar**: Full request schemas now visible
- **No frontend changes needed**: RuleSetWriteRequest interface already matches the typed model structure
