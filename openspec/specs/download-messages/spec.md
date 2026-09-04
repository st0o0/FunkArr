# Download Messages

## Purpose

Commands, queries, responses, and status enum for the download domain. All messages live in `FunkArr.Messages.Download`.

## Requirements

### Requirement: DownloadStatus enum
The system SHALL define a `DownloadStatus` enum with values `Queued`, `Processing`, `Completed`, `Failed` in `FunkArr.Messages.Download`.

#### Scenario: Enum values
- **WHEN** the DownloadStatus enum is inspected
- **THEN** it SHALL contain four values: `Queued` (0), `Processing` (1), `Completed` (2), `Failed` (3)
- **AND** these values SHALL remain unchanged for SABnzbd API compatibility

### Requirement: AddDownload command
The system SHALL define an `AddDownload` record containing all metadata needed to start a download, extracted from the NZB file. The Title SHALL be the scene-style formatted release title.

#### Scenario: AddDownload fields
- **WHEN** an AddDownload message is created
- **THEN** it SHALL contain `Title` (string, scene-formatted), `VideoUrl` (string), `SubtitleUrl` (string?, nullable), `Channel` (string), `Duration` (int, seconds), `Size` (long, bytes), `Category` (string)

#### Scenario: Title is scene-formatted
- **WHEN** an AddDownload is created from a parsed NZB
- **THEN** the Title SHALL already be a scene-style string (e.g. `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`)
- **AND** the DownloadManager SHALL use this title directly for the output filename by appending `.mkv`

### Requirement: DownloadAdded response
The system SHALL define a `DownloadAdded` record returned after a download is accepted into the queue.

#### Scenario: DownloadAdded fields
- **WHEN** a DownloadAdded message is created
- **THEN** it SHALL contain `DownloadId` (Guid) and SHALL implement `IWithDownloadId`

### Requirement: InitDownload command
The system SHALL define an `InitDownload` record sent from Manager to Worker to initialize the Worker with all download metadata. Infrastructure paths (IncompletePath, OutputPath) SHALL NOT be included — the Worker computes these at runtime.

#### Scenario: InitDownload fields
- **WHEN** an InitDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (string)
- **AND** it SHALL implement `IWithDownloadId`
- **AND** it SHALL NOT contain `IncompletePath` or `OutputPath`

### Requirement: StartDownload command
The system SHALL define a `StartDownload` record as a bare go-signal from Manager to Worker containing only the DownloadId.

#### Scenario: StartDownload fields
- **WHEN** a StartDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid) only
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: CancelDownload command
The system SHALL define a `CancelDownload` record sent from Manager to Worker to cancel an active download and passivate the Worker.

#### Scenario: CancelDownload fields
- **WHEN** a CancelDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: ResetDownload command
The system SHALL define a `ResetDownload` record sent from Manager to Worker to reset a Failed Worker back to Initialized for retry.

#### Scenario: ResetDownload fields
- **WHEN** a ResetDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: DownloadStarted persistence DTO
The system SHALL define a `DownloadInitialized` persistence DTO in `FunkArr.Persistence.Events.Download` for the Worker's initialization event. Infrastructure paths SHALL NOT be persisted.

#### Scenario: DownloadInitialized fields
- **WHEN** a DownloadInitialized event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (string)
- **AND** it SHALL NOT contain `IncompletePath` or `OutputPath`

### Requirement: DownloadSucceeded persistence DTO
The system SHALL define a `DownloadSucceeded` persistence DTO for the Worker's successful completion event. It SHALL NOT contain a file path.

#### Scenario: DownloadSucceeded fields
- **WHEN** a DownloadSucceeded event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `DownloadTimeSeconds` (int), `CompletedAt` (long, Unix timestamp)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: DownloadFailed persistence DTO
The system SHALL define a `DownloadFailed` persistence DTO in `FunkArr.Persistence.Events.Download` for the Worker's failure event. This is distinct from the `DownloadFailed` message in `FunkArr.Messages.Download`.

#### Scenario: DownloadFailed DTO fields
- **WHEN** a DownloadFailed persistence event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Reason` (string)

### Requirement: QueryQueue query
The system SHALL define a `QueryQueue` record for requesting the current download queue state with optional pagination and category filter.

#### Scenario: QueryQueue fields
- **WHEN** a QueryQueue message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (string?, nullable, default null)

### Requirement: QueueResult response
The system SHALL define a `QueueResult` record containing the current queue state with pagination metadata.

#### Scenario: QueueResult fields
- **WHEN** a QueueResult message is created
- **THEN** it SHALL contain `Items` (array of `QueueItem`), `TotalSlots` (int), and `TotalItems` (int)

#### Scenario: QueueItem fields
- **WHEN** a QueueItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Status` (DownloadStatus), `TotalBytes` (long), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `Category` (string)

### Requirement: QueryHistory query
The system SHALL define a `QueryHistory` record for requesting download history with optional pagination and category filter.

#### Scenario: QueryHistory fields
- **WHEN** a QueryHistory message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (string?, nullable, default null)

### Requirement: HistoryResult response
The system SHALL define a `HistoryResult` record containing completed and failed downloads with pagination metadata.

#### Scenario: HistoryResult fields
- **WHEN** a HistoryResult message is created
- **THEN** it SHALL contain `Items` (array of `HistoryItem`) and `TotalItems` (int)

#### Scenario: HistoryItem fields
- **WHEN** a HistoryItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `TotalBytes` (long), `DownloadTimeSeconds` (int), `RelativePath` (string), `Status` (DownloadStatus), `FailMessage` (string), `CompletedAt` (long, Unix timestamp)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: DeleteDownload command
The system SHALL define a `DeleteDownload` record for removing an item from queue or history, with an optional flag to delete associated files.

#### Scenario: DeleteDownload fields
- **WHEN** a DeleteDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid) and `DeleteFiles` (bool, default false)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: DeleteDownloadResult response
The system SHALL define a `DeleteDownloadResult` record indicating success or failure of deletion.

#### Scenario: DeleteDownloadResult fields
- **WHEN** a DeleteDownloadResult message is created
- **THEN** it SHALL contain `Success` (bool) and `Error` (string?, nullable)

### Requirement: RetryDownload command
The system SHALL define a `RetryDownload` record for re-queuing a failed download.

#### Scenario: RetryDownload fields
- **WHEN** a RetryDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid) and SHALL implement `IWithDownloadId`

### Requirement: RetryDownloadResult response
The system SHALL define a `RetryDownloadResult` record indicating success or failure of retry.

#### Scenario: RetryDownloadResult fields
- **WHEN** a RetryDownloadResult message is created
- **THEN** it SHALL contain `Success` (bool) and `Error` (string?, nullable)

### Requirement: SlotFree message
The system SHALL define a `SlotFree` record sent from Worker to Manager when a download completes or fails, signaling that the concurrency slot is free.

#### Scenario: SlotFree fields
- **WHEN** a `SlotFree` message is created
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: QueryWorkerStatus query
The system SHALL define a `QueryWorkerStatus` record sent from Manager to Worker to request current state and progress.

#### Scenario: QueryWorkerStatus fields
- **WHEN** a `QueryWorkerStatus` message is created
- **THEN** it SHALL contain `DownloadId` (Guid)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: WorkerStatusResult response
The system SHALL define a `WorkerStatusResult` record returned by the Worker containing full current state and live progress, without a file path.

#### Scenario: WorkerStatusResult fields
- **WHEN** a `WorkerStatusResult` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (int), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `FailMessage` (string?)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: RecordDownload message
The system SHALL define a `RecordDownload` record sent from Worker to HistoryActor when a download completes or fails, carrying `RelativePath` instead of absolute `FilePath`.

#### Scenario: RecordDownload fields
- **WHEN** a `RecordDownload` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (DownloadStatus), `RelativePath` (string?), `FailMessage` (string?), `DownloadTimeSeconds` (int), `CompletedAt` (long)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: RemoveHistoryEntry command
The system SHALL define a `RemoveHistoryEntry` record sent to the HistoryActor to delete a history entry.

#### Scenario: RemoveHistoryEntry fields
- **WHEN** a `RemoveHistoryEntry` message is created
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: DownloadEnqueued persistence DTO
The system SHALL define a `DownloadEnqueued` persistence DTO for the Manager's queue add event.

#### Scenario: DownloadEnqueued fields
- **WHEN** a `DownloadEnqueued` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: DownloadDispatched persistence DTO
The system SHALL define a `DownloadDispatched` persistence DTO for the Manager's dispatch event.

#### Scenario: DownloadDispatched fields
- **WHEN** a `DownloadDispatched` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: DownloadDequeued persistence DTO
The system SHALL define a `DownloadDequeued` persistence DTO for the Manager's queue remove event.

#### Scenario: DownloadDequeued fields
- **WHEN** a `DownloadDequeued` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: HistoryRecorded persistence DTO
The system SHALL define a `HistoryRecorded` persistence DTO for the HistoryActor's record event. It SHALL store `RelativePath` instead of absolute `FilePath`.

#### Scenario: HistoryRecorded fields
- **WHEN** a `HistoryRecorded` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (int), `RelativePath` (string?), `FailMessage` (string?), `DownloadTimeSeconds` (int), `CompletedAt` (long)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: HistoryRemoved persistence DTO
The system SHALL define a `HistoryRemoved` persistence DTO for the HistoryActor's delete event.

#### Scenario: HistoryRemoved fields
- **WHEN** a `HistoryRemoved` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: IDownloadHistoryManager marker interface
The system SHALL define an `IDownloadHistoryManager` marker interface for resolving the DownloadHistoryManager via Servus actor registry.

#### Scenario: Actor resolution
- **WHEN** the DownloadHistoryManager needs to be resolved
- **THEN** it SHALL be resolved via `Context.GetActor<IDownloadHistoryManager>()` or `registry.Get<IDownloadHistoryManager>()`
