## MODIFIED Requirements

### Requirement: QueueResult response
The system SHALL define a `QueueResult` record containing the current queue state with pagination metadata.

#### Scenario: QueueResult fields
- **WHEN** a QueueResult message is created
- **THEN** it SHALL contain `Items` (array of `QueueItem`), `TotalSlots` (int), and `TotalItems` (int)

### Requirement: HistoryResult response
The system SHALL define a `HistoryResult` record containing completed and failed downloads with pagination metadata.

#### Scenario: HistoryResult fields
- **WHEN** a HistoryResult message is created
- **THEN** it SHALL contain `Items` (array of `HistoryItem`) and `TotalItems` (int)

### Requirement: IDownloadHistory marker interface
The system SHALL define an `IDownloadHistoryManager` marker interface for resolving the DownloadHistoryManager via Servus actor registry.

#### Scenario: Actor resolution
- **WHEN** the DownloadHistoryManager needs to be resolved
- **THEN** it SHALL be resolved via `Context.GetActor<IDownloadHistoryManager>()` or `registry.Get<IDownloadHistoryManager>()`

## REMOVED Requirements

### Requirement: IDownloadResponse marker interface
**Reason**: The interface is never used as an `Ask<T>` type constraint. All callers use concrete response types (`DownloadAdded`, `QueueResult`, `DeleteDownloadResult`, etc.). The marker adds no value.
**Migration**: Remove `: IDownloadResponse` from `DownloadAdded`, `QueueResult`, `HistoryResult`, `DeleteDownloadResult`, `RetryDownloadResult`, and `WorkerStatusResult`. No callers need updating.
