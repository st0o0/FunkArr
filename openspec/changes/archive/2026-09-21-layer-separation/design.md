## Context

FunkArr follows a multi-project architecture with domain isolation. Three "contract" projects define the data boundaries:

- **FunkArr.Messages** — commands, queries, responses exchanged between actors
- **FunkArr.Persistence** — events and snapshots stored in Akka journal/snapshot store (SQLite via Newtonsoft.Json)
- **FunkArr.Api** (Models) — DTOs serialized to HTTP clients (System.Text.Json)

Currently Persistence references Messages directly and stores Messages-owned types in the journal. Api.Models uses Messages enums as field types. Domain state holds Messages types directly. This creates hidden coupling where a change to a Messages type can break persistence deserialization or API contracts.

The reference project (Njord) demonstrates the target: each layer owns all its types, connected only by explicit mapping methods.

## Goals / Non-Goals

**Goals:**
- Remove `FunkArr.Persistence` → `FunkArr.Messages` project reference
- Every type in Persistence is owned by Persistence (no transitive Messages dependencies)
- Every type in Api.Models is owned by Api (no Messages enums/records as field types)
- Domain state records hold own internal types, not Messages types directly
- Explicit `ToPersistence()`/`FromPersistence()`, `ToApi()`/`FromApi()`, `ToMessage()`/`FromMessage()` mapping at every boundary
- Serialization attributes live only in the layer that serializes (`[JsonStringEnumConverter]` in Api only, Newtonsoft attributes in Persistence if needed)

**Non-Goals:**
- Changing runtime behavior or API response shapes
- Moving existing types to FunkArr.Core (no shared kernel — each layer owns its copy)
- Changing Akka persistence serialization (stays Newtonsoft.Json default)
- Adding Verify tests (separate change)

## Decisions

### 1. No shared enum kernel — full copy per layer

**Decision:** Each layer owns its own copy of every enum it uses. No shared FunkArr.Core for enums.

**Rationale:** Shared enums are the thin edge of the coupling wedge. If `MediaType` lives in Core, adding a new value requires coordinating across all layers simultaneously. With per-layer copies, each layer can evolve independently — Persistence never changes (extend-only), Messages can add values, Api can expose a subset.

**Trade-off:** More boilerplate mapping code. Acceptable at this project's scale (~9 enums, simple int-backed).

### 2. Persistence ItemTrace tree — flat Persisted* records

**Decision:** Create `PersistedItemTrace`, `PersistedRuleTrace`, `PersistedFilterGroupTrace`, `PersistedFilterNodeTrace`, `PersistedTracedIdentification`, `PersistedEnrichmentTrace` as independent records in `FunkArr.Persistence/Events/ScoringHistory/`.

**Rationale:** The Messages `ItemTrace` tree is complex (6 nested record types). The Persistence copies mirror the structure but are independently versioned and extend-only. Field names match for Newtonsoft.Json compatibility with existing journal entries.

**Migration:** Existing journal entries were serialized from Messages types via Newtonsoft.Json. The Persisted* copies must have identical JSON property names to ensure backward-compatible deserialization. Use `[JsonProperty("fieldName")]` where Newtonsoft defaults differ from the record parameter names.

### 3. Api enum copies with string serialization

**Decision:** Api enum copies get `[JsonStringEnumConverter]` and `[JsonStringEnumMemberName]` attributes. Messages enums become plain (no serialization attributes). Persistence enums serialize as integers (Newtonsoft default).

**Rationale:** Each layer's serialization format is its own concern:
- Api → System.Text.Json with string enum names (human-readable JSON for frontend)
- Persistence → Newtonsoft.Json with integer values (compact, Akka default)
- Messages → No serialization (in-memory actor messages, never serialized directly)

### 4. State isolation — lightweight internal records

**Decision:** Domain state records that currently store Messages types get internal record types. These are private to the domain assembly.

| State | Current | Target |
|---|---|---|
| `ScoringManagerState` | Stores `ImmutableDictionary<string, MatchingConfig>` (Messages) | Internal `ScoringConfig` record, mapped from `MatchingConfig` via `FromMessage()` |
| `StatsCollectorState` | `FromSnapshot(AllStatsResult)` takes Messages type | Internal `StatsEntry` record, mapped via `FromMessage()` |
| `RuleSetResolverState` | `Apply(RegisterRuleSet)` stores Messages command data | Internal `ResolvedRuleSet` record extracted from command fields |

### 5. Mapping method location — extension methods on source type

**Decision:** Mapping methods are extension methods on the source type, located in the consuming project:
- `ToPersistence()` on Messages types → defined in Persistence project (but Persistence no longer references Messages, so these live in the domain actor that does the mapping)
- `ToApi()` on Messages types → defined in Api extensions
- `FromPersistence()` on Persistence types → defined in domain projects
- `ToMessage()` on state types → defined in domain projects

For the Persistence decoupling specifically: the domain actor (e.g., `HistoryWorker`) that calls `Persist()` is responsible for converting between Messages types and Persistence types, since it already references both.

## Risks / Trade-offs

- **[Risk] Existing journal entries use Messages type names as Newtonsoft type hints** → Akka Persistence.Sql uses Newtonsoft without type hints by default (no `$type` field). Serialization is positional/name-based. Persisted* copies with matching property names will deserialize correctly. Verify with Verify tests (separate change).
- **[Risk] Enum value mismatch between layers** → All enum copies start with identical integer values. Persistence enums are extend-only (never reorder). Document that adding a new Messages enum value requires adding it to each layer's copy.
- **[Risk] Large diff** → Mitigate by implementing in order: Persistence first (most critical), then Api, then State. Each step compiles independently.
- **[Trade-off] More boilerplate** → Acceptable for v0.x. If this grows unwieldy, consider source generators in the future.
