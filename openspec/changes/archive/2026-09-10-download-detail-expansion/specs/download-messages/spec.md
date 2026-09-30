## MODIFIED Requirements

### Requirement: WorkerStatusResult response
The system SHALL define a `WorkerStatusResult` record returned by the Worker containing full current state and live progress, without a file path.

#### Scenario: WorkerStatusResult fields
- **WHEN** a `WorkerStatusResult` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (int), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `FailMessage` (string?), `Channel` (string), `HasSubtitles` (bool)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: QueueResult response
The system SHALL define a `QueueResult` record containing the current queue state with pagination metadata.

#### Scenario: QueueResult fields
- **WHEN** a QueueResult message is created
- **THEN** it SHALL contain `Items` (array of `QueueItem`), `TotalSlots` (int), and `TotalItems` (int)

#### Scenario: QueueItem fields
- **WHEN** a QueueItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Status` (DownloadStatus), `TotalBytes` (long), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `Category` (string), `Channel` (string), `HasSubtitles` (bool)
