## Context

The scoring trace model has three string-typed fields that represent fixed sets of values:

1. `FilterGroupTrace.Operator` / `EvaluateGroupTraced` parameter - always `"All"`, `"Any"`, or `"Not"`
2. `IdentificationTrace.Strategy` - always the `.ToString()` of an existing `IdentificationStrategy` enum, or `"AirdateExtraction"` literal
3. `IdentificationTrace.Detail` - always one of 8 fixed failure reason strings, or null on success

These strings flow through four layers: Messages (domain records) -> Scoring (engine) -> Persistence (persisted DTOs) -> Api (API response models). Each layer has its own copy of the records, all using strings.

## Goals / Non-Goals

**Goals:**
- Replace the three string-typed fields with proper enums in the Messages layer
- Keep persistence DTOs as strings (extend-only, wire-compatible)
- Update API models to use string serialization of the new enums (JSON output stays unchanged)
- Update mapping layers to convert between enum and string representations

**Non-Goals:**
- Changing persistence DTOs to use enums (extend-only rule, wire format must not change)
- Changing the API JSON output format (consumers depend on current string values)
- Refactoring the ScoringEngine beyond the string-to-enum replacements

## Decisions

### New enums in FunkArr.Messages

**FilterGroupOp** (`FunkArr.Messages/Scoring/FilterGroupOp.cs`):
```
enum FilterGroupOp { All, Any, Not }
```

**IdentificationFailureReason** (`FunkArr.Messages/Scoring/History/IdentificationFailureReason.cs`):
```
enum IdentificationFailureReason
{
    UnknownStrategy,
    SeasonPatternNotMatched,
    NoEpisodePatternConfigured,
    EpisodePatternNotMatched,
    NoTitlePartsConfigured,
    TitlePartRegexNotMatched,
    TitleDoesNotMatch,
    NoDateFoundInTitle
}
```

No need for a persisted counterpart of either new enum - persistence DTOs keep `string` and the mapping layer converts via `.ToString()` / `Enum.Parse`.

### IdentificationTrace.Strategy reuses existing enum

`IdentificationStrategy` already exists. Change `IdentificationTrace.Strategy` from `string?` to `IdentificationStrategy?`. The engine currently does `spec.Strategy.ToString()` and `"AirdateExtraction"` literal - both become direct enum references. `AirdateExtraction` is already a member of `IdentificationStrategy`.

### Persistence stays string-typed

Persistence DTOs are extend-only. `PersistedFilterGroupTrace.Operator` stays `string`, `PersistedIdentificationTrace.Strategy` stays `string?`, `PersistedIdentificationTrace.Detail` stays `string?`. The `PersistenceMapping` layer converts:
- `FilterGroupOp` <-> `string` via `.ToString()` / `Enum.Parse<FilterGroupOp>()`
- `IdentificationStrategy?` <-> `string?` via `.ToString()` / `Enum.Parse<IdentificationStrategy>()`
- `IdentificationFailureReason?` <-> `string?` via `.ToString()` / `Enum.Parse<IdentificationFailureReason>()`

### API models keep string for JSON stability

`FunkArr.Api.Models.FilterGroupTrace.Operator` stays `string`, `IdentificationTrace.Strategy` stays `string?`, `IdentificationTrace.Detail` stays `string?`. The `ScoringMappingExtensions` maps enum -> string via `.ToString()`. This keeps the JSON response format unchanged for UI consumers.

## Risks / Trade-offs

- **Risk: Existing persisted data uses string values** -> No impact. Persistence DTOs stay string-typed, existing data deserializes as before. Mapping layer handles conversion.
- **Risk: API JSON format change breaks UI** -> Mitigated by keeping API models string-typed. `.ToString()` produces the same values as the current string literals.
- **Trade-off: Two string-to-enum boundaries (persistence, API)** -> Accepted. This is the standard pattern in the project (see `RuleOutcome`, `MatchMethod`). Type safety where logic runs, strings at serialization boundaries.
