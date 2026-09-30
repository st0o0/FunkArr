## Why

After the scoring-engine-string-to-enum refactor, three persistence DTO fields remain string-typed where every other persisted enum uses a dedicated `Persisted*` enum with `(int)` cast mapping. `PersistedFilterGroupTrace.Operator` is `string`, `PersistedIdentificationTrace.Strategy` is `string?`, and `PersistedIdentificationTrace.Detail` is `string?`. This is inconsistent with `PersistedRuleOutcome`, `PersistedMatchMethod`, and `PersistedSearchSource` which are all proper enums. The current `Enum.Parse<>()` / `.ToString()` mapping is fragile compared to the `(int)` cast pattern used everywhere else.

Version 0.x allows breaking persistence without migration code.

## What Changes

- **BREAKING**: New `PersistedFilterGroupOp` enum replacing `string Operator` in `PersistedFilterGroupTrace`
- **BREAKING**: New `PersistedIdentificationStrategy` enum replacing `string? Strategy` in `PersistedIdentificationTrace`
- **BREAKING**: New `PersistedIdentificationFailureReason` enum replacing `string? Detail` in `PersistedIdentificationTrace`
- PersistenceMapping updated to use `(int)` cast pattern instead of `Enum.Parse` / `.ToString()`
- TestItemTraceBuilder updated for new enum types

## Capabilities

### New Capabilities

_None_

### Modified Capabilities

- `scoring-trace-model`: PersistedFilterGroupTrace and PersistedIdentificationTrace use enum fields instead of strings

## Impact

- `FunkArr.Persistence/Events/ScoringHistory/` - modified DTOs, new enum files
- `FunkArr.History/PersistenceMapping.cs` - simplified mapping (int cast instead of string parse)
- `FunkArr.Tests.Shared/TestItemTraceBuilder.cs` - updated for enum values
- **BREAKING**: Existing SQLite journal/snapshot data with string values will not deserialize. Requires DB reset (expected for 0.x dev environment).
