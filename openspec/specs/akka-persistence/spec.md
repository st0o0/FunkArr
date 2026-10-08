## Purpose

Akka.NET persistence configuration: SQLite journal/snapshot storage, persistence health checks, and actor system liveness verification.

## Requirements

### Requirement: SQLite persistence via WithSqlPersistence
`FunkArrActorSystemSetup` SHALL configure Akka.Persistence.Sql with SQLite using
`WithSqlPersistence(connectionString, ProviderName.SQLiteMS, autoInitialize: true)`.
The connection string SHALL be built from `FunkArrOptions.PersistencePath` as
`Data Source={Path.GetFullPath(persistencePath)}`.

#### Scenario: SQLite database created on startup
- **WHEN** the host boots
- **THEN** a SQLite database file exists at the path specified by `FunkArrOptions.PersistencePath`

#### Scenario: Default persistence path
- **WHEN** no `FunkArr:PersistencePath` configuration is provided
- **THEN** the database is created at `data/funkarr.db` relative to the working directory

### Requirement: Persistence health checks
Both the journal and snapshot builders SHALL register ASP.NET health checks via
`.WithHealthCheck()`. These health checks SHALL be aggregated by the existing
`/healthz` endpoint.

#### Scenario: Healthy persistence reported
- **WHEN** `GET /healthz` is requested and the SQLite database is accessible
- **THEN** the response status is 200

#### Scenario: Unhealthy persistence reported
- **WHEN** the SQLite database file is inaccessible or corrupt
- **THEN** `GET /healthz` returns 503

### Requirement: Actor system liveness check
`FunkArrActorSystemSetup` SHALL call `.WithActorSystemLivenessCheck()` to register
a health check that verifies the actor system is running. It SHALL NOT use
`WithAkkaClusterReadinessCheck()` (single-node deployment).

#### Scenario: Actor system liveness reported
- **WHEN** `GET /healthz` is requested and the actor system is running
- **THEN** the liveness check passes

### Requirement: Persist callback contains only state mutation
Persist callbacks SHALL contain only state updates (`_state = _state.Apply(e)`)
and optional interval-based SaveSnapshot. Non-trivial side-effects SHALL use
DeferAsync.

#### Scenario: Handler with multiple Tell targets
- **WHEN** a Persist handler needs to notify 2+ actors after state update
- **THEN** the Tell calls SHALL be in a DeferAsync block, not in the Persist callback

#### Scenario: Handler with single response
- **WHEN** a Persist handler only needs a single Sender.Tell(response) after state update
- **THEN** the Sender.Tell MAY remain inline in the Persist callback

#### Scenario: Handler starting external process
- **WHEN** a Persist handler needs to start an external process (FFmpeg, HTTP)
- **THEN** the process launch SHALL be in a DeferAsync block

#### Scenario: Handler with DispatchNext
- **WHEN** a Persist handler triggers further work via DispatchNext
- **THEN** DispatchNext SHALL be in a DeferAsync block

### Requirement: SaveSnapshot for unbounded-growth actors
Persistent actors with unbounded event growth (singletons that accumulate
events over their lifetime) SHALL use interval-based SaveSnapshot.
Bounded-lifecycle entities with a small, predictable event count are exempt.

#### Scenario: Unbounded singleton
- **WHEN** a persistent singleton actor processes events indefinitely
- **THEN** it SHALL call SaveSnapshot every N events using `LastSequenceNr % SnapshotInterval == 0`

#### Scenario: Bounded-lifecycle entity
- **WHEN** a sharded entity has a finite lifecycle with a small, predictable event count
- **THEN** SaveSnapshot is not required

### Requirement: No ContinueWith in actor code
Actor code SHALL NOT use Task.ContinueWith(). PipeTo with success/failure
parameters or async helper methods SHALL be used instead.

#### Scenario: Fan-out with error handling
- **WHEN** multiple Ask calls need individual error handling before aggregation
- **THEN** a helper method with try/catch SHALL be used instead of ContinueWith

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
