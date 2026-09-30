## Why

The API layer has accumulated inconsistencies: enum values serialized as hardcoded strings, mapper logic scattered across endpoint classes and duplicated between RuleSetMerger and TestRuleMapper, dates as strings instead of DateTimeOffset, and a stale handwritten OpenAPI spec. The `IdentificationStrategy` enum has a 5→3 mapping that causes duplicate switch blocks in two files. At 0.x this is the right time for a clean break.

## What Changes

- Replace all string-typed enum fields in API models with proper enums (int-serialized): QueueStatus, HistoryStatus, SourceType, MediaType, CheckStatus
- Make Api.Models.RuleOutcome eigenständig with explicit ToApi() mapping instead of fragile integer cast
- Expand `IdentificationStrategy` from 3 to 5 members, eliminating `TitleMatchMode` entirely
- Add `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` to Messages enums (FilterField, FilterOp, TitlePartType, IdentificationStrategy) for automatic JSON ruleset file deserialization
- Move all mapper logic from endpoint classes into `ToApi()`/`ToMessage()` extension methods in `Extensions/` directory
- Delete `TestRuleMapper.cs` (replaced by ToMessage() extensions)
- Fix date types: `DownloadHistoryItem.CompletedAt` and `RuleSetListEntry.LastScoringRun` → DateTimeOffset
- Unify `registry.GetAsync<>()` everywhere, extract shared GatewayTimeout helper
- Delete stale `openapi/funkArr-v1.yaml`
- **BREAKING**: API enum fields change from string to int, date fields change format

## Capabilities

### New Capabilities
- `api-enum-types`: Proper int-serialized enum types for all API model fields currently using strings
- `api-mapping-extensions`: ToApi()/ToMessage() extension method pattern for all message↔API model transformations

### Modified Capabilities
- `matching-config`: IdentificationStrategy expanded to 5 members, TitleMatchMode removed, JsonStringEnumConverter on Messages enums
- `matchmagic-evaluation`: Evaluation logic updated for 5-member IdentificationStrategy (no TitleMatchMode switch)
- `api-request-models`: TestScoreRequest mapped via ToMessage() extensions instead of TestRuleMapper
- `api-response-models`: All string-typed enum/date fields replaced with proper types

## Impact

- **FunkArr.Messages**: IdentificationStrategy expanded, TitleMatchMode deleted, JsonStringEnumConverter on FilterField/FilterOp/TitlePartType/IdentificationStrategy
- **FunkArr.RuleSet**: RuleSetMerger simplified — RawRule uses enum properties, ParseFilterField/ParseFilterOp removed
- **FunkArr.MatchMagic**: Evaluation updated for 5-member strategy enum
- **FunkArr.Api**: All endpoint classes simplified (thin routes), new Extensions/ directory, TestRuleMapper deleted, API models get proper enum types
- **FunkArr.Api.Tests**: Tests updated for new enum types and extension methods
- **Frontend**: Will need follow-up change to handle int-based enums instead of string comparisons
- **openapi/**: Stale spec deleted
