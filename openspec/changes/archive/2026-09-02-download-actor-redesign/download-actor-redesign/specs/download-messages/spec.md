## ADDED Requirements

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
The system SHALL define a `WorkerStatusResult` record returned by the Worker containing full current state and live progress.

#### Scenario: WorkerStatusResult fields
- **WHEN** a `WorkerStatusResult` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (WorkerStatus), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `FilePath` (string?), `FailMessage` (string?)

### Requirement: RecordDownload message
The system SHALL define a `RecordDownload` record sent from Worker to HistoryActor when a download completes or fails.

#### Scenario: RecordDownload fields
- **WHEN** a `RecordDownload` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (DownloadStatus), `FilePath` (string?), `FailMessage` (string?), `DownloadTimeSeconds` (int), `CompletedAt` (long)

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
The system SHALL define a `HistoryRecorded` persistence DTO for the HistoryActor's record event.

#### Scenario: HistoryRecorded fields
- **WHEN** a `HistoryRecorded` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (int), `FilePath` (string?), `FailMessage` (string?), `DownloadTimeSeconds` (int), `CompletedAt` (long)

### Requirement: HistoryRemoved persistence DTO
The system SHALL define a `HistoryRemoved` persistence DTO for the HistoryActor's delete event.

#### Scenario: HistoryRemoved fields
- **WHEN** a `HistoryRemoved` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: IDownloadHistory marker interface
The system SHALL define an `IDownloadHistory` marker interface for resolving the DownloadHistoryActor via Servus actor registry.

#### Scenario: Actor resolution
- **WHEN** the DownloadHistoryActor needs to be resolved
- **THEN** it SHALL be resolved via `Context.GetActor<IDownloadHistory>()` or `registry.Get<IDownloadHistory>()`

## MODIFIED Requirements

### Requirement: DownloadStatus enum
The system SHALL define a `DownloadStatus` enum with values `Queued`, `Processing`, `Completed`, `Failed` in `FunkArr.Messages.Download`.

#### Scenario: Enum values
- **WHEN** the DownloadStatus enum is inspected
- **THEN** it SHALL contain four values: `Queued` (0), `Processing` (1), `Completed` (2), `Failed` (3)
- **AND** these values SHALL remain unchanged for SABnzbd API compatibility

### Requirement: IDownloadResponse marker interface
The system SHALL define an `IDownloadResponse` marker interface implemented by all download response messages.

#### Scenario: Marker interface implementations
- **WHEN** download response types are inspected
- **THEN** `DownloadAdded`, `QueueResult`, `HistoryResult`, `DeleteDownloadResult`, `RetryDownloadResult`, and `WorkerStatusResult` SHALL implement `IDownloadResponse`

## REMOVED Requirements

### Requirement: DownloadProgress event
**Reason**: Progress is no longer pushed from Worker to Manager. The Worker stores progress in-memory and returns it via `QueryWorkerStatus`.
**Migration**: Use `QueryWorkerStatus` / `WorkerStatusResult` to retrieve progress data on demand.

### Requirement: DownloadCompleted event
**Reason**: Replaced by `SlotFree` (Worker→Manager) and `RecordDownload` (Worker→HistoryActor). The single message is split into two purpose-specific messages.
**Migration**: Manager receives `SlotFree`. HistoryActor receives `RecordDownload` with completion data.

### Requirement: DownloadFailed event
**Reason**: Replaced by `SlotFree` (Worker→Manager) and `RecordDownload` (Worker→HistoryActor) with Failed status.
**Migration**: Manager receives `SlotFree`. HistoryActor receives `RecordDownload` with failure data.

### Requirement: DownloadRegistered persistence DTO
**Reason**: Replaced by `DownloadEnqueued` which contains only the DownloadId. The Manager no longer stores Title, Category, or Size.
**Migration**: Use `DownloadEnqueued(DownloadId)`.

### Requirement: DownloadInitialized persistence DTO
**Reason**: This DTO belongs to the Worker domain and is not changing. It remains as-is but is no longer listed under download-messages since it was never a message — it's a Worker persistence event. Its definition stays in the download-worker spec.
**Migration**: No change needed. The DTO remains in `FunkArr.Persistence.Events.Download`.
