# match-history-persistence

## Purpose

Defines the ScoringHistoryWorker sharded entity actor: persistence of scoring events, bounded in-memory state, Akka.Persistence snapshots, retention policy, passivation, and configuration.

## Requirements

### Requirement: ScoringHistoryWorker is a sharded entity actor

The ScoringHistoryWorker SHALL be a sharded entity actor keyed by RuleSetId. It SHALL use the naming convention `*Worker` (sharded entity) and be registered in the actor system under shard region "scoring-history".

The ScoringHistoryWorker PersistenceId SHALL be correctly initialized without dead field references. It SHALL use the format `"scoring-history-{ruleSetId}"` derived from `Context.Self.Path.Name`.

#### Scenario: Shard routing by RuleSetId

- **WHEN** a RecordScoringResult message with RuleSetId="tatort" is sent to the ScoringHistory ShardRegion
- **THEN** it SHALL be routed to the ScoringHistoryWorker instance for "tatort"

#### Scenario: Different RuleSets have independent workers

- **WHEN** RecordScoringResult messages arrive for "tatort" and "heute-show"
- **THEN** each SHALL be handled by a separate ScoringHistoryWorker instance with independent state

#### Scenario: PersistenceId format

- **WHEN** a ScoringHistoryWorker is created for RuleSetId "tatort"
- **THEN** its PersistenceId SHALL be "scoring-history-tatort"
- **AND** the PersistenceId SHALL NOT reference a dead `field` keyword or uninitialized backing field

### Requirement: ScoringHistoryWorker persists scoring events

The ScoringHistoryWorker SHALL persist each RecordScoringResult as a `ScoringRecorded` domain event to the Akka.Persistence journal. It SHALL use the new pattern: Command -> `State.ProcessCommand(cmd)` -> `(new State, Event)` -> `Persist(event)` -> assign new state.

#### Scenario: Persist scoring result

- **WHEN** a RecordScoringResult message is received
- **THEN** the ScoringHistoryWorker SHALL call `_state.ProcessCommand(cmd)`, persist the returned `ScoringRecorded`, and update `_state` to the returned new state

#### Scenario: Persist failure does not crash actor

- **WHEN** persistence fails (e.g., SQLite write error)
- **THEN** the ScoringHistoryWorker SHALL log the error and continue accepting new messages (supervision handles restart if needed)

### Requirement: ScoringHistoryWorker maintains bounded in-memory state

The ScoringHistoryWorker SHALL maintain a list of ScoringSnapshot records in memory, bounded by the retention policy. State SHALL be represented as `ScoringHistoryState` defined in a dedicated `ScoringHistoryState.cs` file, initialized from `ScoringHistoryState.Empty`.

#### Scenario: State after persist

- **WHEN** a ScoringRecorded is persisted
- **THEN** the in-memory state SHALL contain a new ScoringSnapshot derived from the event, and retention trimming SHALL be applied via state extension methods

#### Scenario: State on recovery

- **WHEN** a ScoringHistoryWorker recovers from journal
- **THEN** it SHALL replay all events via `_state = _state.Apply(evt)`, apply retention trimming, and be ready to accept new messages

### Requirement: ScoringHistoryWorker takes Akka.Persistence snapshots

The ScoringHistoryWorker SHALL save an Akka.Persistence snapshot every N events (configurable, default 20) using `LastSequenceNr % snapshotInterval == 0`. The state record SHALL be passed directly to `SaveSnapshot()`. On recovery, it SHALL cast the snapshot to `ScoringHistoryState` and assign it directly.

#### Scenario: Snapshot after interval

- **WHEN** `LastSequenceNr % snapshotInterval == 0` after persisting an event
- **THEN** it SHALL call `SaveSnapshot(_state)`

#### Scenario: Recovery with snapshot

- **WHEN** a ScoringHistoryWorker recovers and a SnapshotOffer is received
- **THEN** it SHALL assign `_state = (ScoringHistoryState)offer.Snapshot` and replay only events after the snapshot

#### Scenario: Snapshot interval configurable

- **WHEN** appsettings.json has `FunkArr:ScoringHistory:SnapshotInterval` set to 10
- **THEN** snapshots SHALL be taken when `LastSequenceNr % 10 == 0`

### Requirement: Retention policy trims old snapshots

The ScoringHistoryWorker SHALL enforce a dual retention policy: maximum snapshot count AND maximum age. Both are configurable via `appsettings.json`. Whichever limit triggers first wins.

#### Scenario: Max count exceeded

- **WHEN** MaxSnapshots is 100 and the worker has 101 snapshots in state
- **THEN** the oldest snapshot SHALL be removed, leaving 100

#### Scenario: Max age exceeded

- **WHEN** MaxAgeDays is 30 and a snapshot has Timestamp older than 30 days
- **THEN** that snapshot SHALL be removed regardless of count

#### Scenario: Both limits applied

- **WHEN** MaxSnapshots is 100 and MaxAgeDays is 30 and there are 50 snapshots but 10 are older than 30 days
- **THEN** the 10 old snapshots SHALL be removed, leaving 40

#### Scenario: Trimming on recovery

- **WHEN** a ScoringHistoryWorker recovers and replayed state contains snapshots exceeding retention
- **THEN** retention trimming SHALL be applied before the actor becomes ready

#### Scenario: Default retention values

- **WHEN** no retention config is specified in appsettings.json
- **THEN** MaxSnapshots SHALL default to 100 and MaxAgeDays SHALL default to 30

### Requirement: ScoringHistoryWorker passivates after inactivity

The ScoringHistoryWorker SHALL passivate (stop itself, releasing memory) after 5 minutes of inactivity. It SHALL be re-activated on the next message via shard region.

#### Scenario: Passivation after idle

- **WHEN** no messages arrive for 5 minutes
- **THEN** the ScoringHistoryWorker SHALL request passivation from the shard region

#### Scenario: Re-activation

- **WHEN** a message arrives for a passivated ScoringHistoryWorker
- **THEN** the shard region SHALL create a new instance, which recovers from the journal

### Requirement: ScoringHistoryWorker configuration

The ScoringHistoryWorker SHALL read configuration from `FunkArr:ScoringHistory` section in appsettings.json.

#### Scenario: Configuration structure

- **WHEN** appsettings.json contains `{"FunkArr": {"ScoringHistory": {"MaxSnapshots": 200, "MaxAgeDays": 60, "SnapshotInterval": 10}}}`
- **THEN** the ScoringHistoryWorker SHALL use MaxSnapshots=200, MaxAgeDays=60, and SnapshotInterval=10
