# match-history-persistence

## Purpose

Defines the MatchHistoryWorker sharded entity actor: persistence of scoring events, bounded in-memory state, Akka.Persistence snapshots, retention policy, passivation, and configuration.

## Requirements

### Requirement: MatchHistoryWorker is a sharded entity actor

The MatchHistoryWorker SHALL be a sharded entity actor keyed by RuleSetId. It SHALL use the naming convention `*Worker` (sharded entity) and be registered in the actor system under shard region "match-history".

#### Scenario: Shard routing by RuleSetId

- **WHEN** a RecordScoringResult message with RuleSetId="tatort" is sent to the MatchHistory ShardRegion
- **THEN** it SHALL be routed to the MatchHistoryWorker instance for "tatort"

#### Scenario: Different RuleSets have independent workers

- **WHEN** RecordScoringResult messages arrive for "tatort" and "heute-show"
- **THEN** each SHALL be handled by a separate MatchHistoryWorker instance with independent state

#### Scenario: PersistenceId format

- **WHEN** a MatchHistoryWorker is created for RuleSetId "tatort"
- **THEN** its PersistenceId SHALL be "match-history-tatort"

### Requirement: MatchHistoryWorker persists scoring events

The MatchHistoryWorker SHALL persist each RecordScoringResult as a ScoringRecordedDto event to the Akka.Persistence journal. It SHALL use the FunkArr persistence pattern: Command -> Actor -> Persistence DTO -> State update.

#### Scenario: Persist scoring result

- **WHEN** a RecordScoringResult message is received
- **THEN** the MatchHistoryWorker SHALL map it to a ScoringRecordedDto and persist it, then update in-memory state

#### Scenario: Persist failure does not crash actor

- **WHEN** persistence fails (e.g., SQLite write error)
- **THEN** the MatchHistoryWorker SHALL log the error and continue accepting new messages (supervision handles restart if needed)

### Requirement: MatchHistoryWorker maintains bounded in-memory state

The MatchHistoryWorker SHALL maintain a list of ScoringSnapshot records in memory, bounded by the retention policy. State SHALL be represented as an explicit `sealed record State(ImmutableList<ScoringSnapshot> Snapshots)`.

#### Scenario: State after persist

- **WHEN** a ScoringRecordedDto is persisted
- **THEN** the in-memory state SHALL contain a new ScoringSnapshot derived from the DTO, and retention trimming SHALL be applied

#### Scenario: State on recovery

- **WHEN** a MatchHistoryWorker recovers from journal
- **THEN** it SHALL replay all events (applying each to state), apply retention trimming, and be ready to accept new messages

### Requirement: MatchHistoryWorker takes Akka.Persistence snapshots

The MatchHistoryWorker SHALL save an Akka.Persistence snapshot every N events (configurable, default 20). On recovery, it SHALL load the latest snapshot first, then replay events after the snapshot.

#### Scenario: Snapshot after interval

- **WHEN** the MatchHistoryWorker has persisted 20 events since the last snapshot (or since start)
- **THEN** it SHALL save a snapshot of current state

#### Scenario: Recovery with snapshot

- **WHEN** a MatchHistoryWorker recovers and a snapshot exists at sequence number 40
- **THEN** it SHALL load the snapshot and replay only events after sequence number 40

#### Scenario: Snapshot interval configurable

- **WHEN** appsettings.json has `FunkArr:MatchHistory:SnapshotInterval` set to 10
- **THEN** snapshots SHALL be taken every 10 events

### Requirement: Retention policy trims old snapshots

The MatchHistoryWorker SHALL enforce a dual retention policy: maximum snapshot count AND maximum age. Both are configurable via `appsettings.json`. Whichever limit triggers first wins.

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

- **WHEN** a MatchHistoryWorker recovers and replayed state contains snapshots exceeding retention
- **THEN** retention trimming SHALL be applied before the actor becomes ready

#### Scenario: Default retention values

- **WHEN** no retention config is specified in appsettings.json
- **THEN** MaxSnapshots SHALL default to 100 and MaxAgeDays SHALL default to 30

### Requirement: MatchHistoryWorker passivates after inactivity

The MatchHistoryWorker SHALL passivate (stop itself, releasing memory) after 5 minutes of inactivity. It SHALL be re-activated on the next message via shard region.

#### Scenario: Passivation after idle

- **WHEN** no messages arrive for 5 minutes
- **THEN** the MatchHistoryWorker SHALL request passivation from the shard region

#### Scenario: Re-activation

- **WHEN** a message arrives for a passivated MatchHistoryWorker
- **THEN** the shard region SHALL create a new instance, which recovers from the journal

### Requirement: MatchHistoryWorker configuration

The MatchHistoryWorker SHALL read configuration from `FunkArr:MatchHistory` section in appsettings.json.

#### Scenario: Configuration structure

- **WHEN** appsettings.json contains `{"FunkArr": {"MatchHistory": {"MaxSnapshots": 200, "MaxAgeDays": 60, "SnapshotInterval": 10}}}`
- **THEN** the MatchHistoryWorker SHALL use MaxSnapshots=200, MaxAgeDays=60, and SnapshotInterval=10
