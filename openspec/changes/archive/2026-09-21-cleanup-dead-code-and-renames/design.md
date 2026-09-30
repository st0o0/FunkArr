## Context

The codebase follows the Pathfinder pattern: actor state records with `GetSnapshot()` (caller-facing), `GetPersistenceState()` (Akka journal/snapshot store), and `FromSnapshot()`/`FromPersistence()` for reconstruction. During development, snapshot boilerplate was added to actors that never use it — either because they return responses directly via dedicated message types, or because they're not persistent actors at all.

Additionally, several Messages types carry names from the persistence domain (`AllStatsSnapshot`, `StatsUpdated`, `HistorySnapshot`) but serve completely different roles, creating confusion about the architectural boundaries.

## Goals / Non-Goals

**Goals:**
- Remove all dead snapshot types and their associated methods
- Remove the dead `ScoringRecorded` persistence event
- Rename misleading types to match their actual role
- Keep the solution compiling and all tests passing

**Non-Goals:**
- Adding SaveSnapshot support to Download actors (stays event-only)
- Layer separation refactoring (separate change)
- Adding new tests (separate change)
- Changing any runtime behavior

## Decisions

### 1. Remove dead snapshots entirely vs mark obsolete

**Decision:** Remove entirely.

**Rationale:** Version 0.x — no compatibility concerns. The types are never called from production code. Marking obsolete would preserve dead code that creates false impressions about the architecture.

### 2. Download actors: event-only persistence

**Decision:** Keep Download actors (`DownloadManager`, `DownloadWorker`, `DownloadHistoryManager`) as event-only persistent actors. Do not add SaveSnapshot support.

**Rationale:** Individual downloads have bounded event chains (Enqueued → Initialized → Started → Succeeded/Faulted). Recovery replays a small, fixed number of events per entity. The cost of full event replay is negligible for these actors.

### 3. Rename strategy

**Decision:** Straightforward renames with find-and-replace across the solution.

| Current | New | Convention |
|---|---|---|
| `StatsUpdated` | `UpdateStats` | VerbNoun for commands |
| `AllStatsSnapshot` | `AllStatsResult` | NounResult for query responses |
| `HistoryState.HistorySnapshot` | `HistoryState.HistoryEntry` | Descriptive — it's a history entry, not an Akka snapshot |

### 4. Test handling

**Decision:** Remove tests that exclusively test dead code. Update tests that reference renamed types. Do not add replacement tests (covered by the tests change).

**Files affected:**
- `DownloadStateSnapshotTests` — remove snapshot roundtrip tests for dead types, keep any other tests in the file
- `ScoringManagerStateTests` — remove snapshot roundtrip assertions
- Any test referencing renamed types — update references

## Risks / Trade-offs

- **[Risk] Missed reference to dead type** → Compiler will catch it. Run `dotnet build src/FunkArr.slnx` after each removal step.
- **[Risk] ScoringRecorded still in existing journals** → This is a persistence event that may exist in SQLite journals from prior runs. Removing the C# type means Akka cannot deserialize old entries. However, `ScoringRecorded` was replaced by `HistoryRecorded` (with `EnrichedCount`), and any production data was migrated. If old journal entries exist, Akka will log a deserialization warning but continue (events are replayed best-effort). At v0.x this is acceptable.
- **[Risk] Rename breaks external consumers** → The renamed types (`UpdateStats`, `AllStatsResult`) are internal actor messages, not exposed via HTTP API. No external impact.
