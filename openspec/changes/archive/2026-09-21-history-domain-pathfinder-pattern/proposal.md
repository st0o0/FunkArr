## Why

History (recording, stats aggregation, query projections) is an independent domain mixed into FunkArr.Scoring. Meanwhile, actor state management uses "state-as-snapshot" (passing state records directly to `SaveSnapshot`), coupling internal state evolution to the persistence format. The Akka.Pathfinder pattern separates these concerns: internal state stays private, a dedicated persisted-state record handles serialization, and callers receive purpose-built snapshot/response types — never raw state.

## What Changes

- **Extract FunkArr.History domain**: Move `HistoryWorker`, `HistoryState`, `StatsCollector`, `StatsCollectorState` from `FunkArr.Scoring` into new `FunkArr.History` project. Move corresponding messages from `FunkArr.Messages/Scoring/History/` to `FunkArr.Messages/History/`.
- **Delete ScoringHistoryWorker**: `ScoringHistoryWorker` and `ScoringHistoryState` are unused legacy (replaced by `HistoryWorker`). Remove them and their tests.
- **Adopt Pathfinder state pattern on all stateful actors**: Every actor with a state class gets `GetSnapshot()` + `FromSnapshot()` on the state. Persistent actors additionally get `GetPersistenceState()` + `FromPersistence()` with a separate `Persisted*State` record in `FunkArr.Persistence`. Download domain actors stay replay-only (no snapshots).
- **BREAKING**: `actor-state-management` spec changes from "state-as-snapshot" to "persisted-state-record" pattern. Persistent actors no longer pass state directly to `SaveSnapshot()`.
- **Normalize state mutation naming**: All state mutation methods use `Apply()` convention consistently (replace `Increment`/`Decrement`, `AddPending`/`RemovePending` etc.).

## Capabilities

### New Capabilities
- `history-domain`: History domain boundary — project structure, actor registration, message namespace, and dependency direction for HistoryWorker, StatsCollector, and their messages.

### Modified Capabilities
- `actor-state-management`: Replace "state-as-snapshot" with Pathfinder pattern — `GetSnapshot()`/`FromSnapshot()` on all stateful actors, `GetPersistenceState()`/`FromPersistence()` + `Persisted*State` records for persistent actors.
- `stats-collector`: Update message types (`AllStatsSnapshot` instead of `AllStatsResult`), snapshot pattern on state, namespace move to History domain.
- `match-history-persistence`: HistoryWorker gets separate `PersistedHistoryState` record, `GetPersistenceState()`/`FromPersistence()` on state, namespace move to History domain.

## Impact

- **New project**: `FunkArr.History` + `FunkArr.History.Tests`
- **Deleted files**: `ScoringHistoryWorker.cs`, `ScoringHistoryState.cs`, `ScoringHistoryWorkerTests.cs`, `ScoringHistoryStateTests.cs`
- **Moved files**: HistoryWorker, StatsCollector and their states from Scoring to History; History messages from Messages/Scoring/History to Messages/History
- **Modified actors** (13 stateful actors across all domains): state classes get snapshot methods added
- **Modified persistent actors** (HistoryWorker, DownloadManager, DownloadHistoryManager, DownloadWorker): persistence pattern changes. Download actors stay replay-only but get `GetSnapshot()`/`FromSnapshot()` for consistency.
- **Persistence**: New `Persisted*State` records in `FunkArr.Persistence/Events/` for HistoryWorker
- **AkkaSetupContainer**: Actor registrations updated for History domain project references
- **Architecture tests**: New project boundary rules for FunkArr.History
- **No API changes**: External API contracts unchanged
