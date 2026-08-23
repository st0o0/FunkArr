## Purpose

Cluster-sharded persistent actors that track per-download request lifecycle (status, timestamps, output path) independently of the download queue. Each entity is addressed by nzoId and serves SABnzbd queue/history queries.

## Requirements

### Requirement: Single-node Cluster Sharding infrastructure
The system SHALL configure Akka.Cluster with single-node seed and register a DownloadRequestTracker ShardRegion. Entity IDs SHALL be nzoIds extracted from messages via `IWithNzoId` interface.

#### Scenario: Cluster starts on single node
- **WHEN** the application starts
- **THEN** the ActorSystem SHALL join a single-node cluster and the DownloadRequestTracker ShardRegion SHALL be available

#### Scenario: Entity addressed by nzoId
- **WHEN** a message implementing `IWithNzoId` with `NzoId = "abc123"` is sent to the ShardRegion
- **THEN** it SHALL be routed to the DownloadRequestTracker entity with entityId "abc123"

### Requirement: DownloadRequestTracker entity persistence
Each `DownloadRequestTracker` entity SHALL be a `ReceivePersistentActor` with `PersistenceId: "download-request-{nzoId}"`. It SHALL persist events: `RequestCreated`, `StatusChanged`, `Completed`, `Failed`.

#### Scenario: Request created on first message
- **WHEN** a `CreateRequest(nzoId, title, downloadUrl)` message arrives for a new entity
- **THEN** the tracker SHALL persist `RequestCreated` and initialize its state

#### Scenario: Status survives restart
- **WHEN** a tracker entity restarts after a crash
- **THEN** it SHALL replay events and restore its last known status

### Requirement: Status query for SABnzbd queue
Each tracker entity SHALL respond to `GetStatus` with its current nzoId, title, status, and timestamps.

#### Scenario: Active download status query
- **WHEN** `GetStatus` is received for an entity in "Downloading" state
- **THEN** the tracker SHALL reply with `StatusResponse(nzoId, title, "Downloading", enqueuedAt)`

### Requirement: History entry query for SABnzbd history
Each tracker entity SHALL respond to `GetHistoryEntry` with its nzoId, title, final status, output path, completion time, and error message.

#### Scenario: Completed download history query
- **WHEN** `GetHistoryEntry` is received for an entity in "Completed" state
- **THEN** the tracker SHALL reply with `HistoryEntryResponse(nzoId, title, "Completed", outputPath, completedAt, null)`

### Requirement: Status updates from DownloadQueueActor
The tracker SHALL accept `UpdateStatus(nzoId, status)`, `MarkCompleted(nzoId, outputPath)`, and `MarkFailed(nzoId, error)` messages to update its state with persisted events.

#### Scenario: Status changed to Downloading
- **WHEN** `UpdateStatus("abc123", "Downloading")` is received
- **THEN** the tracker SHALL persist `StatusChanged` and update its in-memory status

#### Scenario: Mark completed
- **WHEN** `MarkCompleted("abc123", "/output/file.mkv")` is received
- **THEN** the tracker SHALL persist `Completed` with the output path and timestamp

### Requirement: QueueCoordinator creates tracker on Enqueue
When `QueueCoordinator` enqueues a new job, it SHALL tell the DownloadRequestTracker ShardRegion with `CreateRequest(nzoId, title, downloadUrl)` to create the tracker entity.

#### Scenario: Tracker created alongside queue entry
- **WHEN** QueueCoordinator processes an Enqueue command
- **THEN** it SHALL tell the ShardRegion with `CreateRequest` containing the nzoId and job metadata
