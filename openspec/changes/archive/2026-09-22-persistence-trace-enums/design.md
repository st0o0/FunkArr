## Context

The project has two patterns for persisting enums:

1. **Enum-to-enum with `(int)` cast** (used by `RuleOutcome`, `MatchMethod`, `SearchSource`): Domain enum -> `(PersistedX)(int)domainValue` -> stored as integer. Recovery: `(DomainX)(int)persistedValue`. Safe against renames, compact storage.

2. **Enum-to-string with `ToString()` / `Enum.Parse<>()`** (used by `FilterGroupOp`, `IdentificationStrategy`, `IdentificationFailureReason`): Domain enum -> `.ToString()` -> stored as string. Recovery: `Enum.Parse<>()`. Fragile on renames, verbose storage.

Pattern 2 was introduced as a transitional step during the string-to-enum refactor. This change aligns everything to pattern 1.

## Goals / Non-Goals

**Goals:**
- Introduce `PersistedFilterGroupOp`, `PersistedIdentificationStrategy`, `PersistedIdentificationFailureReason` enums in `FunkArr.Persistence`
- Change `PersistedFilterGroupTrace.Operator` from `string` to `PersistedFilterGroupOp`
- Change `PersistedIdentificationTrace.Strategy` from `string?` to `PersistedIdentificationStrategy?`
- Change `PersistedIdentificationTrace.Detail` from `string?` to `PersistedIdentificationFailureReason?`
- Update PersistenceMapping to use `(int)` cast pattern
- Consistent enum persistence across all scoring trace types

**Non-Goals:**
- Migration of existing persisted data (0.x, DB reset acceptable)
- Changing API models (they stay string-typed for JSON stability)

## Decisions

### Enum member ordering matches domain enums

Each `Persisted*` enum mirrors the member order of its domain counterpart so `(int)` cast produces correct values. This is the established pattern (`PersistedRuleOutcome` mirrors `RuleOutcome`, etc.).

### Nullable enums use nullable persisted field

`PersistedIdentificationTrace.Strategy` and `.Detail` become `PersistedIdentificationStrategy?` and `PersistedIdentificationFailureReason?`. The mapping handles null: `strategy is not null ? (PersistedIdentificationStrategy)(int)strategy.Value : null`.

## Risks / Trade-offs

- **Risk: Existing data breaks** - Accepted. Version 0.x, dev environment runs via Docker Compose, DB reset is a `docker compose down -v`.
- **Risk: Enum member reorder breaks persistence** - Same risk as all other persisted enums in the project. Mitigated by extend-only convention (append new members, never reorder).
