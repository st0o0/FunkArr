## MODIFIED Requirements

### Requirement: HistoryWorker persists history events

The HistoryWorker SHALL persist each RecordHistory command as a `HistoryRecorded` event to the Akka.Persistence journal using `_state.ProcessCommand(cmd)` -> `(new State, Event)` -> `Persist(event)` -> assign new state. The HistoryRecorded event SHALL use `PersistedDownloadCompletion` for completion data fields instead of flat fields, where applicable.

#### Scenario: Persist history record
- **WHEN** a RecordHistory message is received
- **THEN** the HistoryWorker SHALL call `_state.ProcessCommand(cmd)`, persist the returned `HistoryRecorded` containing embedded sub-records (PersistedDownloadCompletion, PersistedScoreCandidate), and update `_state` to the returned new state

### Requirement: HistoryWorker uses Pathfinder persistence pattern

The HistoryWorker SHALL use a separate `PersistedHistoryState` record for Akka.Persistence snapshots. The `HistoryState` SHALL implement `GetPersistenceState()` returning `PersistedHistoryState` and a static `FromPersistence(PersistedHistoryState)` factory for recovery. The `PersistedHistoryState` record SHALL reside in `FunkArr.Persistence/Events/ScoringHistory/`. Embedded sub-records (PersistedScoreCandidate, PersistedDownloadCompletion where used) SHALL be preserved through snapshot serialization.

#### Scenario: Save snapshot via GetPersistenceState
- **WHEN** `LastSequenceNr % snapshotInterval == 0` after persisting an event
- **THEN** the actor SHALL call `SaveSnapshot(_state.GetPersistenceState())` with embedded persistence sub-records

#### Scenario: Recover from snapshot via FromPersistence
- **WHEN** a `SnapshotOffer` is received during recovery with a `PersistedHistoryState`
- **THEN** the actor SHALL call `HistoryState.FromPersistence(persisted)` to reconstruct the state with embedded sub-records mapped to domain types

#### Scenario: PersistedHistoryState is flat data
- **WHEN** `PersistedHistoryState` is examined
- **THEN** it SHALL be a sealed record with only the data fields needed to reconstruct HistoryState - no methods, no trimming logic
