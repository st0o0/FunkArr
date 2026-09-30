## ADDED Requirements

### Requirement: RecentMatchActor registration
The system SHALL provide a `RecentMatchActor` registered via Akka.Hosting under the name `"recent-match-actor"` as a `ReceivePersistentActor` singleton with `PersistenceId = "recent-match-actor"`.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the `RecentMatchActor` SHALL be registered and resolvable via IActorRegistry

#### Scenario: Recovery restores state
- **WHEN** the actor restarts and recovers from its journal
- **THEN** the recent records and unmatched aggregations SHALL be restored

### Requirement: Record match results
The actor SHALL accept `RecordMatch(MatchRecord)` messages and persist each as a `MatchRecordAdded` event.

#### Scenario: Record a match
- **WHEN** `RecordMatch` is received with a MatchRecord
- **THEN** the actor SHALL persist `MatchRecordAdded` and add the record to the ring buffer

#### Scenario: Ring buffer overflow
- **WHEN** the ring buffer contains 100 records and a new `RecordMatch` arrives
- **THEN** the oldest record SHALL be evicted and the new record added

### Requirement: Recent records query
The actor SHALL respond to `GetRecent(int Limit)` with the most recent match records in reverse chronological order.

#### Scenario: Get recent with default limit
- **WHEN** `GetRecent(50)` is received and 80 records exist
- **THEN** the actor SHALL reply with the 50 most recent records sorted by timestamp descending

#### Scenario: Get recent from empty buffer
- **WHEN** `GetRecent(50)` is received and no records exist
- **THEN** the actor SHALL reply with an empty list

### Requirement: Unmatched items aggregation
The actor SHALL maintain unmatched items grouped by topic, capped at 50 items per topic, updated from each incoming MatchRecord.

#### Scenario: Get unmatched all topics
- **WHEN** `GetUnmatched(null)` is received
- **THEN** the actor SHALL reply with all unmatched groups sorted by item count descending

#### Scenario: Get unmatched filtered by topic
- **WHEN** `GetUnmatched("Tatort")` is received
- **THEN** the actor SHALL reply with only the unmatched group for Tatort

#### Scenario: Per-topic cap
- **WHEN** a topic accumulates more than 50 unmatched items
- **THEN** the oldest items SHALL be evicted to maintain the 50-item cap

### Requirement: Snapshot support
The actor SHALL save a snapshot every 50 persisted events containing the full state.

#### Scenario: Snapshot taken
- **WHEN** 50 events have been persisted since the last snapshot
- **THEN** the actor SHALL save a snapshot

#### Scenario: Recovery from snapshot
- **WHEN** the actor recovers and a snapshot exists
- **THEN** it SHALL restore from the snapshot and replay only subsequent events
