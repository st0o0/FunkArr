## Why

The ScoringEngine and its trace model use magic strings where proper enums should be. `FilterGroupTrace.Operator` takes `"All"`, `"Any"`, `"Not"` as strings; `IdentificationTrace.Strategy` converts an existing enum to string via `.ToString()`; `IdentificationTrace.Detail` uses free-text failure reasons from a fixed set. This creates a stringly-typed surface that the compiler can't check and the serializer can't validate.

## What Changes

- New `FilterGroupOp` enum replacing `string Operator` in `FilterGroupTrace` and the `string op` parameter through `EvaluateGroupTraced`
- `IdentificationTrace.Strategy` changed from `string?` to `IdentificationStrategy?` (enum already exists)
- New `IdentificationFailureReason` enum replacing `string? Detail` in `IdentificationTrace`
- Update ScoringEngine to use enum values instead of string literals
- Update tests to match new enum-typed trace fields

## Capabilities

### New Capabilities

_None - this refactor introduces no new capabilities._

### Modified Capabilities

- `scoring-trace-model`: FilterGroupTrace.Operator becomes `FilterGroupOp` enum, IdentificationTrace.Strategy becomes `IdentificationStrategy?`, IdentificationTrace.Detail becomes `IdentificationFailureReason?`
- `scoring-engine`: Engine implementation updated to use enum values instead of string literals

## Impact

- `FunkArr.Messages/Scoring/` - new enum files, modified trace records
- `FunkArr.Messages/Scoring/History/` - modified `FilterGroupTrace`, `IdentificationTrace`
- `FunkArr.Scoring/ScoringEngine.cs` - switch on enums instead of strings
- `FunkArr.Scoring.Tests/` - test assertions updated for enum values
- `FunkArr.Persistence/` - persistence DTOs may need extend-only updates if traces are persisted
- **BREAKING**: Trace record constructor signatures change (string -> enum)
