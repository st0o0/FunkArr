# match-history-persistence

## Purpose

Defines the HistoryWorker sharded entity actor: persistence of history events, bounded in-memory state, Pathfinder persistence pattern with PersistedHistoryState, retention policy, passivation, and configuration.

## Requirements

### Requirement: HistoryWorker is a sharded entity actor

The HistoryWorker SHALL be a sharded entity actor keyed by RuleSetId in the `FunkArr.History` namespace. It SHALL be registered under shard region "history" with `IHistoryRegion` actor key. Its PersistenceId SHALL be `"history-{ruleSetId}"`.

#### Scenario: Shard routing by RuleSetId
- **WHEN** a RecordHistory message with RuleSetId="tatort" is sent to the History ShardRegion
- **THEN** it SHALL be routed to the HistoryWorker instance for "tatort"

#### Scenario: Different RuleSets have independent workers
- **WHEN** RecordHistory messages arrive for "tatort" and "heute-show"
- **THEN** each SHALL be handled by a separate HistoryWorker instance with independent state

#### Scenario: PersistenceId format
- **WHEN** a HistoryWorker is created for RuleSetId "tatort"
- **THEN** its PersistenceId SHALL be "history-tatort"

### Requirement: HistoryWorker persists history events

The HistoryWorker SHALL persist each RecordHistory command as a `HistoryRecorded` event to the Akka.Persistence journal using `_state.ProcessCommand(cmd)` -> `(new State, Event)` -> `Persist(event)` -> assign new state.

#### Scenario: Persist history record
- **WHEN** a RecordHistory message is received
- **THEN** the HistoryWorker SHALL call `_state.ProcessCommand(cmd)`, persist the returned `HistoryRecorded`, and update `_state` to the returned new state

### Requirement: HistoryWorker uses Pathfinder persistence pattern

The HistoryWorker SHALL use a separate `PersistedHistoryState` record for Akka.Persistence snapshots. The `HistoryState` SHALL implement `GetPersistenceState()` returning `PersistedHistoryState` and a static `FromPersistence(PersistedHistoryState)` factory for recovery. The `PersistedHistoryState` record SHALL reside in `FunkArr.Persistence/Events/ScoringHistory/`.

#### Scenario: Save snapshot via GetPersistenceState
- **WHEN** `LastSequenceNr % snapshotInterval == 0` after persisting an event
- **THEN** the actor SHALL call `SaveSnapshot(_state.GetPersistenceState())`

#### Scenario: Recover from snapshot via FromPersistence
- **WHEN** a `SnapshotOffer` is received during recovery with a `PersistedHistoryState`
- **THEN** the actor SHALL call `HistoryState.FromPersistence(persisted)` to reconstruct the state

#### Scenario: PersistedHistoryState is flat data
- **WHEN** `PersistedHistoryState` is examined
- **THEN** it SHALL be a sealed record with only the data fields needed to reconstruct HistoryState — no methods, no trimming logic

### Requirement: HistoryWorker provides GetSnapshot for queries

The `HistoryState` SHALL implement `GetSnapshot()` and `FromSnapshot()` for query responses. Query handlers SHALL use state projection methods, not expose raw state.

#### Scenario: Stats query uses state projection
- **WHEN** a `QueryScoringStats` is received
- **THEN** the actor SHALL respond with `_state.Stats` (a computed `ScoringStatsResult`), not the raw state record

### Requirement: HistoryWorker maintains bounded in-memory state

The HistoryWorker SHALL maintain history records in memory, bounded by retention policy (max count + max age). Trimming SHALL be applied after persist and on recovery.

#### Scenario: Retention trimming after persist
- **WHEN** a HistoryRecorded is persisted
- **THEN** `_state.Trim(maxSnapshots, maxAgeDays)` SHALL be applied

#### Scenario: Retention trimming on recovery
- **WHEN** a HistoryWorker recovers
- **THEN** retention trimming SHALL be applied before the actor becomes ready

### Requirement: HistoryWorker passivates after inactivity

The HistoryWorker SHALL passivate after 5 minutes of inactivity via `Context.SetReceiveTimeout`.

#### Scenario: Passivation after idle
- **WHEN** no messages arrive for 5 minutes
- **THEN** the HistoryWorker SHALL request passivation from the shard region

### Requirement: ScoringHistoryWorker is removed

The legacy `ScoringHistoryWorker`, `ScoringHistoryState`, and their tests SHALL be deleted. The shard region "scoring-history" SHALL NOT be registered.

#### Scenario: No ScoringHistoryWorker in codebase
- **WHEN** the solution is searched for `ScoringHistoryWorker` or `ScoringHistoryState`
- **THEN** no results SHALL be found
