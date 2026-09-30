## MODIFIED Requirements

### Requirement: DownloadAdded response

The system SHALL define a `DownloadAdded` record returned after a download is accepted into the queue.

#### Scenario: DownloadAdded fields

- **WHEN** a DownloadAdded message is created
- **THEN** it SHALL contain `DownloadId` (Guid) and SHALL implement `IWithDownloadId` and `IDownloadResponse`

### Requirement: QueueResult response

The system SHALL define a `QueueResult` record containing the current queue state with pagination metadata.

#### Scenario: QueueResult fields

- **WHEN** a QueueResult message is created
- **THEN** it SHALL contain `Items` (array of `QueueItem`), `TotalSlots` (int), and `TotalItems` (int)
- **AND** it SHALL implement `IDownloadResponse`

### Requirement: HistoryResult response

The system SHALL define a `HistoryResult` record containing completed and failed downloads with pagination metadata.

#### Scenario: HistoryResult fields

- **WHEN** a HistoryResult message is created
- **THEN** it SHALL contain `Items` (array of `HistoryItem`) and `TotalItems` (int)
- **AND** it SHALL implement `IDownloadResponse`

### Requirement: DeleteDownloadResult response

The system SHALL define a `DeleteDownloadResult` record indicating success or failure of deletion.

#### Scenario: DeleteDownloadResult fields

- **WHEN** a DeleteDownloadResult message is created
- **THEN** it SHALL contain `Success` (bool) and `Error` (string?, nullable)
- **AND** it SHALL implement `IDownloadResponse`

### Requirement: RetryDownloadResult response

The system SHALL define a `RetryDownloadResult` record indicating success or failure of retry.

#### Scenario: RetryDownloadResult fields

- **WHEN** a RetryDownloadResult message is created
- **THEN** it SHALL contain `Success` (bool) and `Error` (string?, nullable)
- **AND** it SHALL implement `IDownloadResponse`

### Requirement: WorkerStatusResult response

The system SHALL define a `WorkerStatusResult` record returned by the Worker containing full current state and live progress, without a file path.

#### Scenario: WorkerStatusResult fields

- **WHEN** a `WorkerStatusResult` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (int), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `FailMessage` (string?), `Channel` (string), `HasSubtitles` (bool)
- **AND** it SHALL implement `IDownloadResponse`
- **AND** it SHALL NOT contain `FilePath`
