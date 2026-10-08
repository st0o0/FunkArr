## ADDED Requirements

### Requirement: Singleton actors clean up journal after snapshot
Persistent singleton actors SHALL delete old journal entries and old snapshots
after a successful snapshot save. On receiving `SaveSnapshotSuccess`, the actor
SHALL call `DeleteMessages(sequenceNr)` to remove all journal events up to the
snapshot's sequence number, and `DeleteSnapshots(sequenceNr - 1)` to remove all
older snapshots. This keeps Postgres bounded at one snapshot plus at most
`SnapshotInterval` journal rows per actor.

#### Scenario: Journal cleanup after snapshot
- **WHEN** a persistent singleton actor receives `SaveSnapshotSuccess` with sequence number 50
- **THEN** the actor SHALL call `DeleteMessages(50)` and `DeleteSnapshots(49)`

#### Scenario: DownloadManager cleans up after snapshot
- **WHEN** `DownloadManager` saves a snapshot at sequence number 75
- **THEN** on `SaveSnapshotSuccess`, it SHALL delete journal entries up to 75 and snapshots before 75

#### Scenario: DownloadHistoryManager cleans up after snapshot
- **WHEN** `DownloadHistoryManager` saves a snapshot at sequence number 50
- **THEN** on `SaveSnapshotSuccess`, it SHALL delete journal entries up to 50 and snapshots before 50

#### Scenario: HistoryWorker cleans up after snapshot
- **WHEN** `HistoryWorker` saves a snapshot at sequence number 40
- **THEN** on `SaveSnapshotSuccess`, it SHALL delete journal entries up to 40 and snapshots before 40

#### Scenario: Bounded-lifecycle entities are exempt
- **WHEN** a sharded entity actor (e.g. `DownloadWorker`) has a small, predictable event count
- **THEN** journal cleanup is not required
