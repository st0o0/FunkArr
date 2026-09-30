## MODIFIED Requirements

### Requirement: DownloadSucceeded persistence DTO
The system SHALL define a `DownloadSucceeded` persistence DTO for the Worker's successful completion event. It SHALL NOT contain a file path.

#### Scenario: DownloadSucceeded fields
- **WHEN** a DownloadSucceeded event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `DownloadTimeSeconds` (int), `CompletedAt` (long, Unix timestamp)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: HistoryRecorded persistence DTO
The system SHALL define a `HistoryRecorded` persistence DTO for the HistoryActor's record event. It SHALL store `RelativePath` instead of absolute `FilePath`.

#### Scenario: HistoryRecorded fields
- **WHEN** a `HistoryRecorded` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long), `Status` (int), `RelativePath` (string?), `FailMessage` (string?), `DownloadTimeSeconds` (int), `CompletedAt` (long)
- **AND** it SHALL NOT contain `FilePath`

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

### Requirement: HistoryResult response
The system SHALL define a `HistoryResult` record containing completed and failed downloads with pagination metadata.

#### Scenario: HistoryResult fields
- **WHEN** a HistoryResult message is created
- **THEN** it SHALL contain `Items` (array of `HistoryItem`) and `TotalItems` (int)

#### Scenario: HistoryItem fields
- **WHEN** a HistoryItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `TotalBytes` (long), `DownloadTimeSeconds` (int), `RelativePath` (string), `Status` (DownloadStatus), `FailMessage` (string), `CompletedAt` (long, Unix timestamp)
- **AND** it SHALL NOT contain `FilePath`
