## MODIFIED Requirements

### Requirement: DownloadRequestTracker entity persistence
Each `DownloadRequestTracker` entity SHALL be a `ReceivePersistentActor` with `PersistenceId: "download-request-{nzoId}"`. It SHALL persist events: `RequestCreated`, `StatusChanged`, `Completed`, `Failed`. The `RequestCreated` event SHALL include an optional `Category` field.

#### Scenario: Request created with category
- **WHEN** a `CreateRequest(nzoId, title, downloadUrl, category: "tv")` message arrives for a new entity
- **THEN** the tracker SHALL persist `RequestCreated` with category `"tv"` and initialize its state

#### Scenario: Request created without category
- **WHEN** a `CreateRequest(nzoId, title, downloadUrl, category: null)` message arrives
- **THEN** the tracker SHALL persist `RequestCreated` with null category

#### Scenario: Status survives restart
- **WHEN** a tracker entity restarts after a crash
- **THEN** it SHALL replay events and restore its last known status including category

### Requirement: Status query for SABnzbd queue
Each tracker entity SHALL respond to `GetStatus` with its current nzoId, title, status, category, and timestamps.

#### Scenario: Active download status query includes category
- **WHEN** `GetStatus` is received for an entity with category `"tv"` in "Downloading" state
- **THEN** the tracker SHALL reply with `StatusResponse(nzoId, title, "Downloading", category: "tv", enqueuedAt)`

### Requirement: History entry query for SABnzbd history
Each tracker entity SHALL respond to `GetHistoryEntry` with its nzoId, title, final status, output path, category, completion time, and error message.

#### Scenario: Completed download history query includes category
- **WHEN** `GetHistoryEntry` is received for a completed entity with category `"movies"`
- **THEN** the tracker SHALL reply with `HistoryEntryResponse(nzoId, title, "Completed", outputPath, category: "movies", completedAt, null)`

### Requirement: QueueCoordinator creates tracker on Enqueue
When `QueueCoordinator` enqueues a new job, it SHALL tell the DownloadRequestTracker ShardRegion with `CreateRequest(nzoId, title, downloadUrl, category)` to create the tracker entity.

#### Scenario: Tracker created with category
- **WHEN** QueueCoordinator processes an Enqueue command with category `"tv"`
- **THEN** it SHALL tell the ShardRegion with `CreateRequest` containing the nzoId, job metadata, and category `"tv"`
