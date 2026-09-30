## MODIFIED Requirements

### Requirement: AddDownload command
The system SHALL define an `AddDownload` record containing all metadata needed to start a download, extracted from the NZB file. The Title SHALL be the scene-style formatted release title.

#### Scenario: AddDownload fields
- **WHEN** an AddDownload message is created
- **THEN** it SHALL contain `Title` (string, scene-formatted), `VideoUrl` (string), `SubtitleUrl` (string?, nullable), `Channel` (string), `Duration` (int, seconds), `Size` (long, bytes), `Category` (MediaType)

#### Scenario: Title is scene-formatted
- **WHEN** an AddDownload is created from a parsed NZB
- **THEN** the Title SHALL already be a scene-style string (e.g. `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`)
- **AND** the DownloadManager SHALL use this title directly for the output filename by appending `.mkv`

### Requirement: InitDownload command
The system SHALL define an `InitDownload` record sent from Manager to Worker to initialize the Worker with all download metadata. Infrastructure paths (IncompletePath, OutputPath) SHALL NOT be included — the Worker computes these at runtime.

#### Scenario: InitDownload fields
- **WHEN** an InitDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (MediaType)
- **AND** it SHALL implement `IWithDownloadId`
- **AND** it SHALL NOT contain `IncompletePath` or `OutputPath`

### Requirement: QueryQueue query
The system SHALL define a `QueryQueue` record for requesting the current download queue state with optional pagination and category filter.

#### Scenario: QueryQueue fields
- **WHEN** a QueryQueue message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (MediaType?, nullable, default null)

### Requirement: QueueResult response
The system SHALL define a `QueueResult` record containing the current queue state with pagination metadata.

#### Scenario: QueueResult fields
- **WHEN** a QueueResult message is created
- **THEN** it SHALL contain `Items` (array of `QueueItem`), `TotalSlots` (int), and `TotalItems` (int)

#### Scenario: QueueItem fields
- **WHEN** a QueueItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Status` (DownloadStatus), `TotalBytes` (long), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `Category` (MediaType), `Channel` (string), `HasSubtitles` (bool)

### Requirement: QueryHistory query
The system SHALL define a `QueryHistory` record for requesting download history with optional pagination and category filter.

#### Scenario: QueryHistory fields
- **WHEN** a QueryHistory message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (MediaType?, nullable, default null)

### Requirement: HistoryResult response
The system SHALL define a `HistoryResult` record containing completed and failed downloads with pagination metadata.

#### Scenario: HistoryItem fields
- **WHEN** a HistoryItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (MediaType), `TotalBytes` (long), `DownloadTimeSeconds` (int), `RelativePath` (string), `Status` (DownloadStatus), `FailMessage` (string), `CompletedAt` (long, Unix timestamp)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: WorkerStatusResult response
The system SHALL define a `WorkerStatusResult` record returned by the Worker containing full current state and live progress, without a file path.

#### Scenario: WorkerStatusResult fields
- **WHEN** a `WorkerStatusResult` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (MediaType), `Size` (long), `Status` (int), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `FailMessage` (string?), `Channel` (string), `HasSubtitles` (bool)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: RecordDownload message
The system SHALL define a `RecordDownload` record sent from Worker to HistoryActor when a download completes or fails, carrying `RelativePath` instead of absolute `FilePath`.

#### Scenario: RecordDownload fields
- **WHEN** a `RecordDownload` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (MediaType), `Size` (long), `Status` (DownloadStatus), `RelativePath` (string?), `FailMessage` (string?), `DownloadTimeSeconds` (int), `CompletedAt` (long)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: DownloadStarted persistence DTO
The system SHALL define a `DownloadInitialized` persistence DTO in `FunkArr.Persistence.Events.Download` for the Worker's initialization event. Infrastructure paths SHALL NOT be persisted.

#### Scenario: DownloadInitialized fields
- **WHEN** a DownloadInitialized event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (MediaType)
- **AND** it SHALL NOT contain `IncompletePath` or `OutputPath`

### Requirement: HistoryRecorded persistence DTO
The system SHALL define a `HistoryRecorded` persistence DTO for the HistoryActor's record event. It SHALL store `RelativePath` instead of absolute `FilePath`.

#### Scenario: HistoryRecorded fields
- **WHEN** a `HistoryRecorded` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (MediaType), `Size` (long), `Status` (int), `RelativePath` (string?), `FailMessage` (string?), `DownloadTimeSeconds` (int), `CompletedAt` (long)
- **AND** it SHALL NOT contain `FilePath`
