## Context

FunkArr has 13 stateful actors across 4 domains. Their state classes follow inconsistent patterns — some use `Apply()`, some use ad-hoc mutation names, and persistent actors pass their state record directly to `SaveSnapshot()` (coupling internal state evolution to the serialization format). The Akka.Pathfinder project (D:\GIT\Akka.Pathfinder) demonstrates a cleaner three-tier separation: mutable state → persisted record → response messages.

Additionally, history/stats actors (`HistoryWorker`, `StatsCollector`) live in `FunkArr.Scoring` but serve a different purpose (recording and querying what happened) than scoring (computing match quality). `ScoringHistoryWorker` is unused legacy code.

## Goals / Non-Goals

**Goals:**
- Extract History as its own domain project with clean boundaries
- Delete unused `ScoringHistoryWorker` + `ScoringHistoryState`
- Establish the Pathfinder state pattern as the universal standard for all stateful actors
- Normalize all state mutation methods to `Apply()` convention

**Non-Goals:**
- Adding Akka persistence snapshots to Download actors (they stay replay-only)
- Changing actor behavior or business logic — this is a structural/pattern refactor
- Migrating existing persisted data — version 0.x allows breaking persistence changes

## Decisions

### D1: Pathfinder three-tier state pattern

Every stateful actor follows this structure:

```
State (sealed record, internal)
├── Apply(msg) → new State                    // mutations
├── ProcessCommand(cmd) → (State, Event)      // persistent actors only
├── GetSnapshot() → SnapshotRecord            // package for callers
├── FromSnapshot(SnapshotRecord) → State      // reconstruct from snapshot
├── GetPersistenceState() → PersistedRecord   // persistent actors only
├── FromPersistence(PersistedRecord) → State  // persistent actors only
└── TryGet*() / To*()                         // query projections (optional)
```

**Types involved per actor:**

| Layer | Type | Location | Purpose |
|-------|------|----------|---------|
| State | `*State` sealed record | Domain project | Internal, never sent outside actor |
| Snapshot | Response record | `FunkArr.Messages` | What callers receive from queries |
| Persisted | `Persisted*State` record | `FunkArr.Persistence` | What goes to `SaveSnapshot()` |

**Rationale over "state-as-snapshot":** State records accumulate helper methods, computed properties, and trimming logic. Persisting them directly means any structural change to the state breaks deserialization. A separate persisted record is a stable contract that can evolve independently (extend-only).

**Alternative considered:** Keep state-as-snapshot. Rejected because we're already hitting the coupling issue — `HistoryState` has `Trim()` and query methods that have nothing to do with persistence.

### D2: Replay-only actors get GetSnapshot/FromSnapshot but no persistence layer

Download actors (`DownloadManager`, `DownloadHistoryManager`, `DownloadWorker`) stay replay-only. They get `GetSnapshot()`/`FromSnapshot()` for consistency (callers never see raw state), but no `GetPersistenceState()`/`FromPersistence()` since they don't save snapshots.

### D3: History domain project structure

```
src/
  FunkArr.History/
    HistoryWorker.cs
    HistoryState.cs
    StatsCollector.cs
    StatsCollectorState.cs
    FunkArr.History.csproj → refs Core, Messages, Persistence

  FunkArr.History.Tests/
    HistoryWorkerTests.cs        (moved from Scoring.Tests)
    HistoryStateTests.cs         (moved from Scoring.Tests)
    StatsCollectorTests.cs       (new, if not existing)
    FunkArr.History.Tests.csproj

  FunkArr.Messages/History/      (moved from Messages/Scoring/History/)
    RecordHistory.cs
    QueryScoringStats.cs
    QueryScoringHistory.cs
    StatsMessages.cs

  FunkArr.Persistence/Events/ScoringHistory/
    PersistedHistoryState.cs     (new — snapshot record for HistoryWorker)
    HistoryRecorded.cs           (stays — event record)
```

`IHistoryRegion`, `IStatsCollector` stay in `FunkArr.Core` (actor keys are always there).

### D4: Apply naming normalization

| Actor State | Current naming | Normalized |
|-------------|---------------|------------|
| MediathekViewWebManagerState | `Increment()`, `Decrement()` | `Apply(MediathekRequestStarted)`, `Apply(MediathekRequestCompleted)` |
| SearchManagerState | `AddPending()`, `UpdatePending()`, `RemovePending()` | `Apply(SearchPending)`, `Apply(SearchUpdated)`, `Apply(SearchCompleted)` |
| RuleSetManagerState | `with {}` in actor | `Apply()` extension methods |

The exact message types for Apply parameters will be defined during implementation — they can be simple internal records in the state file.

### D5: Actor inventory — what each gets

| Actor | Domain | Apply | GetSnapshot | FromSnapshot | GetPersistence | FromPersistence | Persisted Record |
|-------|--------|-------|-------------|-------------|----------------|-----------------|-----------------|
| StatsCollector | History | ✅ exists | ✅ exists | ✅ exists | — | — | — |
| HistoryWorker | History | ✅ exists | + add | + add | + add | + add | + add `PersistedHistoryState` |
| ScoringManager | Scoring | ✅ exists | + add | + add | — | — | — |
| TvSearchWorker | Search | ✅ exists | + add | + add | — | — | — |
| MovieSearchWorker | Search | ✅ exists | + add | + add | — | — | — |
| SearchManager | Search | normalize | + add | + add | — | — | — |
| MediathekViewWebMgr | Search | normalize | + add | + add | — | — | — |
| RuleSetResolver | RuleSet | ✅ exists | + add | + add | — | — | — |
| RuleSetManager | RuleSet | normalize | + add | + add | — | — | — |
| DownloadManager | Download | ✅ exists | + add | + add | — | — | — |
| DownloadHistoryMgr | Download | ✅ exists | + add | + add | — | — | — |
| DownloadWorker | Download | ✅ exists | + add | + add | — | — | — |

## Risks / Trade-offs

**[Risk] Large surface area — 13 actors touched** → Mitigated by mechanical nature of changes (adding methods to state classes). Each actor is independent — can be done and tested incrementally. Architecture tests catch boundary violations.

**[Risk] Namespace move breaks existing Akka persistence journal entries for HistoryWorker** → Not a risk at 0.x (breaking persistence changes are acceptable per project rules). PersistenceId stays the same (`history-{ruleSetId}`), and introducing the separate `PersistedHistoryState` record is a clean break from the old state-as-snapshot format.

**[Trade-off] More types per actor** → Each persistent actor gains one extra record type. Accepted because the decoupling benefit outweighs the boilerplate, and the pattern is mechanical/predictable.

**[Trade-off] Search worker states are mutable classes, not records** → `TvSearchWorkerState` and `MovieSearchWorkerState` use `sealed class` with mutable `Apply()`. Converting them to records is out of scope (different concern, large diff). They get `GetSnapshot()`/`FromSnapshot()` but stay as classes internally.
