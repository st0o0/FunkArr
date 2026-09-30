## ADDED Requirements

### Requirement: Retry info query
Each tracker entity SHALL respond to `QueryRetryInfo(nzoId)` with `RetryInfo(nzoId, downloadUrl, title, subtitleUrl, category)` containing the original download parameters needed to re-enqueue the job. The response SHALL only be valid when the entity is in "Failed" status.

#### Scenario: Query retry info for failed download
- **WHEN** `QueryRetryInfo("abc123")` is received for an entity in "Failed" status with DownloadUrl `https://example.com/video.mp4`
- **THEN** the tracker SHALL reply with `RetryInfo("abc123", "https://example.com/video.mp4", "Show.S01E01", null, "tv")`

#### Scenario: Query retry info for non-failed download
- **WHEN** `QueryRetryInfo("abc123")` is received for an entity in "Downloading" status
- **THEN** the tracker SHALL reply with `RetryInfo` where all fields are populated (the controller validates status)

## MODIFIED Requirements

### Requirement: DownloadRequestActor entity persistence
Each `DownloadRequestActor` entity SHALL be a `ReceivePersistentActor` with `PersistenceId: "download-request-{nzoId}"`. It SHALL persist journal DTOs: `RequestCreated`, `RequestStatusChanged`, `RequestCompleted`, `RequestFailed` (from `FunkArr.Persistence`). Domain events are defined in `DownloadRequestActorEvents`: `RequestCreated`, `StatusChanged`, `Completed`, `Failed`. Persistence uses extension methods `ToJournal()` / `ToDomain()` for DTO conversion. The actor state SHALL store `DownloadUrl` from the `RequestCreated` event for retry support.

#### Scenario: Request created with category
- **WHEN** a `TrackDownload(nzoId, title, downloadUrl, category: "tv", enqueuedAt)` message arrives for a new entity
- **THEN** the tracker SHALL persist `RequestCreated` journal DTO with category `"tv"` and initialize its state including `DownloadUrl`

#### Scenario: Request created without category
- **WHEN** a `TrackDownload(nzoId, title, downloadUrl, category: null, enqueuedAt)` message arrives
- **THEN** the tracker SHALL persist `RequestCreated` journal DTO with null category and store `DownloadUrl` in state

#### Scenario: Status survives restart
- **WHEN** a tracker entity restarts after a crash
- **THEN** it SHALL replay journal DTOs, convert via `ToDomain()`, and restore its last known status including category and `DownloadUrl`

### Requirement: QueueActor creates tracker on Enqueue
When `QueueActor` enqueues a new job, it SHALL tell the DownloadRequestActor ShardRegion with `TrackDownload(nzoId, title, downloadUrl, subtitleUrl, category, enqueuedAt)` to create the tracker entity. The `TrackDownload` message SHALL include `subtitleUrl` for retry support.

#### Scenario: Tracker created with category and subtitle URL
- **WHEN** QueueActor processes an Enqueue command with category `"tv"` and subtitleUrl `"https://example.com/sub.xml"`
- **THEN** it SHALL tell the ShardRegion with `TrackDownload` containing the nzoId, job metadata, subtitleUrl, category `"tv"`, and enqueue timestamp

#### Scenario: Tracker created without subtitle URL
- **WHEN** QueueActor processes an Enqueue command with subtitleUrl null
- **THEN** it SHALL tell the ShardRegion with `TrackDownload` containing null subtitleUrl
