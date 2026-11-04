## Context

Audit of all enums across the FunkArr codebase found ~40 enums not following project conventions. The conventions require explicit int values on Persistence enums and `Unknown = 0` on new enums in all projects. In practice, only Persistence (10/11) and ArrApi (3/3) are compliant. This is a mechanical refactor — no behavior changes, no new features.

## Goals / Non-Goals

**Goals:**
- Every enum member across all projects has an explicit int value
- Enums crossing wire boundaries (Api, Messages) have `Unknown = 0` where applicable
- Mapping switches handle `Unknown` arms correctly
- Zero behavior change — existing numeric values preserved exactly

**Non-Goals:**
- Adding `Unknown` to enums exempt by convention (Priority, FailureReason, actor-internal)
- Changing any serialization format or wire representation
- Adding new ArchUnit tests for enum conventions (separate future work)
- Renumbering existing Persistence enum values

## Decisions

### 1. Explicit ints everywhere, not just Persistence

**Decision**: Add explicit int values to ALL enums, not just Persistence.

**Rationale**: Messages enums map 1:1 to Persistence enums. If a developer reorders a Messages enum, the implicit values shift, and the exhaustive switch still compiles — but the mapping is silently wrong. Explicit ints make reorder a visible diff.

**Alternative considered**: Only fix Persistence (per literal convention). Rejected because the mapping risk is real and the cost of adding ints to Messages/Api is trivial.

### 2. `Unknown = 0` scope

**Decision**: Add `Unknown = 0` to enums that cross wire boundaries (Messages, Api). Exempt:
- **Priority enums** (`DownloadPriority`, `QueuePriority`): `Normal = 0` is semantically correct, always explicitly set
- **FailureReason enums** (4x in `LocalRuleSetCommands.cs`): 1-2 members, always specific
- **Actor-internal enums** (`SearchType`): Never serialized
- **Internal enums** (`SubtitleFailureReason`): Domain-internal

**Rationale**: `Unknown` catches uninitialized `default(T)` and unknown deserialized values. Exemptions are enums where `Unknown` has no semantic meaning or the enum never leaves the process.

### 3. Api `Unknown` handling pattern

**Decision**: Api mapping extensions throw `ArgumentOutOfRangeException` for `Unknown` via the existing `_ => throw` default arm. No special rejection logic needed in endpoint handlers because:
- `Unknown` as a request value: `JsonStringEnumConverter` with camelCase naming — `"unknown"` deserializes to `Unknown`, and the mapping switch throws before reaching domain logic
- `Unknown` as a response value: Should never happen (domain never produces `Unknown`); if it does, the mapping throw is the right signal

### 4. `SourceType.Unknown` repositioning

**Decision**: Move `Unknown` from last position (implicit = 3) to first position with `= 0`. Renumber existing members: `Community = 1, Local = 2, Merged = 3`. This is safe because `SourceType` is an Api-layer enum — not persisted, only serialized as camelCase string on wire.

**Alternative considered**: Keep `Unknown = 3` and just add explicit values. Rejected because convention says `Unknown = 0` so `default(T)` equals `Unknown`.

### 5. Value assignment strategy for existing enums

**Decision**: When adding explicit ints to existing implicit enums, assign each member its current implicit ordinal (0, 1, 2, ...). For enums gaining `Unknown = 0`, shift existing members up by 1.

- **Messages enums gaining Unknown**: `Unknown = 0`, then existing members at `1, 2, 3, ...`
- **Messages enums without Unknown** (Priority, FailureReason): Keep current implicit ordinals as explicit
- **Api enums**: Same pattern — `Unknown = 0`, existing members shifted
- **Domain enums**: Case by case (some get Unknown, some don't)

This preserves wire compatibility for enums already serialized as strings (the int value doesn't matter for JSON string serialization). For Persistence, only `PersistedSearchSource` needs fixing and its values stay the same.

### 6. Enum-to-enum mapping for Unknown

**Decision**: The `_ => throw new ArgumentOutOfRangeException(...)` default arm already handles `Unknown` correctly — no additional mapping arms needed. `Unknown` is not a valid domain value, so it should throw at mapping boundaries.

## Risks / Trade-offs

- **[Risk] Existing journal data with implicit ordinals** → Not a risk: Persistence enums already have explicit values (except `PersistedSearchSource` which gets its implicit ordinals made explicit — no value change)
- **[Risk] Api enum value shift breaks clients** → Not a risk: Api enums serialize as camelCase strings, not ints. The int backing value is invisible on wire
- **[Risk] `SearchSource` in Messages gains Unknown but Persistence `PersistedSearchSource` doesn't** → Mitigated: Mapping switch throws for `Unknown`, preventing it from reaching persistence. Persistence convention says existing enums keep their values
- **[Risk] Large diff touches many files** → Trade-off accepted: Mechanical change, easy to review file-by-file. Each enum is independent — partial merge is safe
