## Purpose

Cluster-sharded persistent actors that track per-download request lifecycle (status, timestamps, output path) independently of the download queue. Each entity is addressed by nzoId and serves SABnzbd queue/history queries.

## Requirements

### Requirement: Single-node Cluster Sharding infrastructure
The system SHALL configure Akka.Cluster with single-node seed and register a DownloadRequestActor ShardRegion using `ShardedMessageExtractor` with `maxNumberOfShards: 10`. Entity IDs SHALL be nzoIds extracted from messages via `IWithNzoId` which extends `IShardedMessage`. The namespace SHALL be `FunkArr.DownloadClient.Tracker`.

#### Scenario: Cluster starts on single node
- **WHEN** the application starts
- **THEN** the ActorSystem SHALL join a single-node cluster and the DownloadRequestActor ShardRegion SHALL use `new ShardedMessageExtractor(10)`

#### Scenario: Entity addressed by nzoId via IShardedMessage
- **WHEN** a message implementing `IWithNzoId` with `NzoId = "abc123"` is sent to the ShardRegion
- **THEN** `IShardedMessage.EntityKey` SHALL return `"abc123"` and the message SHALL be routed to entity `"abc123"`

### Requirement: DownloadRequestActor entity persistence
Each `DownloadRequestActor` entity SHALL be a `ReceivePersistentActor` with `PersistenceId: "download-request-{nzoId}"`. It SHALL persist journal DTOs: `RequestCreated`, `RequestStatusChanged`, `RequestCompleted`, `RequestFailed` (from `FunkArr.Persistence`). Domain events are defined in `DownloadRequestActorEvents`: `RequestCreated`, `StatusChanged`, `Completed`, `Failed`. Persistence uses extension methods `ToJournal()` / `ToDomain()` for DTO conversion.

#### Scenario: Request created with category
- **WHEN** a `TrackDownload(nzoId, title, downloadUrl, category: "tv", enqueuedAt)` message arrives for a new entity
- **THEN** the tracker SHALL persist `RequestCreated` journal DTO with category `"tv"` and initialize its state

#### Scenario: Request created without category
- **WHEN** a `TrackDownload(nzoId, title, downloadUrl, category: null, enqueuedAt)` message arrives
- **THEN** the tracker SHALL persist `RequestCreated` journal DTO with null category

#### Scenario: Status survives restart
- **WHEN** a tracker entity restarts after a crash
- **THEN** it SHALL replay journal DTOs, convert via `ToDomain()`, and restore its last known status including category

### Requirement: Status query for SABnzbd queue
Each tracker entity SHALL respond to `QueryStatus` with its current nzoId, title, status, category, and enqueuedAt timestamp.

#### Scenario: Active download status query includes category
- **WHEN** `QueryStatus` is received for an entity with category `"tv"` in "Downloading" state
- **THEN** the tracker SHALL reply with `DownloadStatus(nzoId, title, "Downloading", category: "tv", enqueuedAt)`

### Requirement: History entry query for SABnzbd history
Each tracker entity SHALL respond to `QueryHistory` with its nzoId, title, final status, category, output path, completion time, and error message.

#### Scenario: Completed download history query includes category
- **WHEN** `QueryHistory` is received for a completed entity with category `"movies"`
- **THEN** the tracker SHALL reply with `DownloadHistoryEntry(nzoId, title, "Completed", category: "movies", outputPath, completedAt, null)`

### Requirement: Status updates from DownloadActor
The tracker SHALL accept `ReportProgress(nzoId, status)`, `CompleteDownload(nzoId, outputPath)`, and `FailDownload(nzoId, error)` messages to update its state with persisted events.

#### Scenario: Status changed to Downloading
- **WHEN** `ReportProgress("abc123", "Downloading")` is received
- **THEN** the tracker SHALL persist `RequestStatusChanged` journal DTO and update its in-memory status

#### Scenario: Mark completed
- **WHEN** `CompleteDownload("abc123", "/output/file.mkv")` is received
- **THEN** the tracker SHALL persist `RequestCompleted` journal DTO with the output path and timestamp

### Requirement: QueueActor creates tracker on Enqueue
When `QueueActor` enqueues a new job, it SHALL tell the DownloadRequestActor ShardRegion with `TrackDownload(nzoId, title, downloadUrl, category, enqueuedAt)` to create the tracker entity.

#### Scenario: Tracker created with category
- **WHEN** QueueActor processes an Enqueue command with category `"tv"`
- **THEN** it SHALL tell the ShardRegion with `TrackDownload` containing the nzoId, job metadata, category `"tv"`, and enqueue timestamp
