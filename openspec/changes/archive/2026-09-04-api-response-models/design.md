## Context

The internal API (`FunkArr.Api`) currently returns Message records directly as JSON. The ArrApi layer already follows the correct pattern — it defines its own wire-format models (`Rss`, `Item`, etc.) and maps from Messages. The internal API should follow the same approach.

Four endpoints in `RuleSetApiEndpoints.cs` return Messages directly:
- `RegisteredRuleSetEntry[]` (list), `RuleSetDetailResult` (detail), `ScoringHistoryResult` (history), `ScoringDetailResult` (scoring detail).

## Goals / Non-Goals

**Goals:**
- Decouple API JSON contract from actor Message types
- API models owned by `FunkArr.Api`, Messages owned by domain actors
- Each can evolve independently

**Non-Goals:**
- Changing the JSON shape visible to the frontend (models start as structural copies)
- Introducing a mapping library (AutoMapper, Mapster) — manual mapping is fine for 4 endpoints
- Adding pagination wrappers or other API-layer concerns (separate change)

## Decisions

### API models as sealed records in `FunkArr.Api/Models/`

API response models live in `FunkArr.Api` as `sealed record` types under a `Models/` folder, namespaced `FunkArr.Api.Models`. One file per endpoint response group.

**Why records:** Consistent with the rest of the codebase. Immutable, concise, good JSON serialization.

**Why not a shared contract project:** The API models are owned by the API layer. No other project should reference them. Adding a shared project would re-introduce the coupling we're removing.

### Inline mapping in endpoint handlers

Each endpoint maps from Message to API model directly in the handler lambda. No separate mapper classes, no extension methods — the mapping is trivial (field-to-field copy) and co-located with the endpoint that uses it.

**Why not extension methods:** The mapping is 1:1 today. Extracting it adds indirection for no gain. If mappings grow complex, we can extract later.

### Keep same JSON shape

The initial API models produce identical JSON output to what the Messages produce today. This is a pure refactoring — no frontend changes needed.

## Risks / Trade-offs

- **Near-duplicate types** — API models will initially mirror Message records field-for-field. This is the intentional trade-off: duplication now buys independence later. → Acceptable; the alternative (keeping coupling) is worse.
- **Mapping maintenance** — When a Message changes, the mapping must be updated manually. → Low risk; there are only 4 endpoints, and a missing field produces a compile error.
