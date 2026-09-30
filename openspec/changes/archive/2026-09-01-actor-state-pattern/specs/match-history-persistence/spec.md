## MODIFIED Requirements

### Requirement: MatchHistoryWorker persists scoring events

The MatchHistoryWorker SHALL persist each RecordScoringResult as a `ScoringRecordedEvent` domain event to the Akka.Persistence journal. It SHALL use the new pattern: Command → `State.ProcessCommand(cmd)` → `(new State, Event)` → `Persist(event)` → assign new state.

#### Scenario: Persist scoring result
- **WHEN** a RecordScoringResult message is received
- **THEN** the MatchHistoryWorker SHALL call `_state.ProcessCommand(cmd)`, persist the returned `ScoringRecordedEvent`, and update `_state` to the returned new state

#### Scenario: Persist failure does not crash actor
- **WHEN** persistence fails (e.g., SQLite write error)
- **THEN** the MatchHistoryWorker SHALL log the error and continue accepting new messages (supervision handles restart if needed)

### Requirement: MatchHistoryWorker maintains bounded in-memory state

The MatchHistoryWorker SHALL maintain a list of ScoringSnapshot records in memory, bounded by the retention policy. State SHALL be represented as `MatchHistoryState` defined in a dedicated `MatchHistoryState.cs` file, initialized from `MatchHistoryState.Empty`.

#### Scenario: State after persist
- **WHEN** a ScoringRecordedEvent is persisted
- **THEN** the in-memory state SHALL contain a new ScoringSnapshot derived from the event, and retention trimming SHALL be applied via state extension methods

#### Scenario: State on recovery
- **WHEN** a MatchHistoryWorker recovers from journal
- **THEN** it SHALL replay all events via `_state = _state.Apply(evt)`, apply retention trimming, and be ready to accept new messages

### Requirement: MatchHistoryWorker takes Akka.Persistence snapshots

The MatchHistoryWorker SHALL save an Akka.Persistence snapshot every N events (configurable, default 20) using `LastSequenceNr % snapshotInterval == 0`. The state record SHALL be passed directly to `SaveSnapshot()`. On recovery, it SHALL cast the snapshot to `MatchHistoryState` and assign it directly.

#### Scenario: Snapshot after interval
- **WHEN** `LastSequenceNr % snapshotInterval == 0` after persisting an event
- **THEN** it SHALL call `SaveSnapshot(_state)`

#### Scenario: Recovery with snapshot
- **WHEN** a MatchHistoryWorker recovers and a SnapshotOffer is received
- **THEN** it SHALL assign `_state = (MatchHistoryState)offer.Snapshot` and replay only events after the snapshot

#### Scenario: Snapshot interval configurable
- **WHEN** appsettings.json has `FunkArr:MatchHistory:SnapshotInterval` set to 10
- **THEN** snapshots SHALL be taken when `LastSequenceNr % 10 == 0`

## REMOVED Requirements

### Requirement: MatchHistoryWorker persists scoring events (old pattern)
**Reason**: Replaced by domain event pattern. The old requirement specified "Command -> Actor -> Persistence DTO -> State update" with `ScoringRecordedDto`. The new pattern uses `ProcessCommand` → domain event → persist.
**Migration**: MatchHistoryWorker now uses `State.ProcessCommand(cmd)` which returns `(MatchHistoryState, ScoringRecordedEvent)`. No DTO mapping step.
