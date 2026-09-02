# Download Messages

## Purpose

Commands, queries, responses, and status enum for the download domain. All messages live in `FunkArr.Messages.Download`.

## Requirements

### Requirement: DownloadStatus enum
The system SHALL define a `DownloadStatus` enum with values `Queued`, `Processing`, `Completed`, `Failed`, `Extracting`, `Moving`, `Verifying` in `FunkArr.Messages.Download`.

#### Scenario: Enum values
- **WHEN** the DownloadStatus enum is inspected
- **THEN** it SHALL contain seven values: `Queued` (0), `Processing` (1), `Completed` (2), `Failed` (3), `Extracting` (4), `Moving` (5), `Verifying` (6)

### Requirement: AddDownload command
The system SHALL define an `AddDownload` record containing all metadata needed to start a download, extracted from the NZB file, including priority.

#### Scenario: AddDownload fields
- **WHEN** an AddDownload message is created
- **THEN** it SHALL contain `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?, nullable), `Channel` (string), `Duration` (int, seconds), `Size` (long, bytes), `Category` (string), `Priority` (int, default 0)

### Requirement: DownloadAdded response
The system SHALL define a `DownloadAdded` record returned after a download is accepted into the queue.

#### Scenario: DownloadAdded fields
- **WHEN** a DownloadAdded message is created
- **THEN** it SHALL contain `DownloadId` (Guid) and SHALL implement `IWithDownloadId`

### Requirement: StartDownload command
The system SHALL define a `StartDownload` record sent from Manager to Worker to begin processing.

#### Scenario: StartDownload fields
- **WHEN** a StartDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `OutputPath` (string)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: DownloadProgress event
The system SHALL define a `DownloadProgress` record reported periodically by the Worker during FFmpeg execution.

#### Scenario: DownloadProgress fields
- **WHEN** a DownloadProgress message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `CurrentTimeUs` (long, microseconds from FFmpeg), `TotalDuration` (int, seconds), `BytesDownloaded` (long), `TotalBytes` (long), `Speed` (double, playback speed multiplier)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: DownloadCompleted event
The system SHALL define a `DownloadCompleted` record sent when FFmpeg exits successfully.

#### Scenario: DownloadCompleted fields
- **WHEN** a DownloadCompleted message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `FilePath` (string, path to output MKV), `DownloadTimeSeconds` (int)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: DownloadFailed event
The system SHALL define a `DownloadFailed` record sent when FFmpeg exits with a non-zero code or an error occurs.

#### Scenario: DownloadFailed fields
- **WHEN** a DownloadFailed message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Reason` (string)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: QueryQueue query
The system SHALL define a `QueryQueue` record for requesting the current download queue state with optional pagination and category filter.

#### Scenario: QueryQueue fields
- **WHEN** a QueryQueue message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (string?, nullable, default null)

### Requirement: QueueResult response
The system SHALL define a `QueueResult` record containing the current queue state.

#### Scenario: QueueResult fields
- **WHEN** a QueueResult message is created
- **THEN** it SHALL contain `Items` (array of `QueueItem`) and `TotalSlots` (int)

#### Scenario: QueueItem fields
- **WHEN** a QueueItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Status` (DownloadStatus), `TotalBytes` (long), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `Category` (string)

### Requirement: QueryHistory query
The system SHALL define a `QueryHistory` record for requesting download history with optional pagination and category filter.

#### Scenario: QueryHistory fields
- **WHEN** a QueryHistory message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (string?, nullable, default null)

### Requirement: HistoryResult response
The system SHALL define a `HistoryResult` record containing completed and failed downloads.

#### Scenario: HistoryResult fields
- **WHEN** a HistoryResult message is created
- **THEN** it SHALL contain `Items` (array of `HistoryItem`)

#### Scenario: HistoryItem fields
- **WHEN** a HistoryItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `TotalBytes` (long), `DownloadTimeSeconds` (int), `FilePath` (string), `Status` (DownloadStatus), `FailMessage` (string), `CompletedAt` (long, Unix timestamp)

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

### Requirement: IDownloadResponse marker interface
The system SHALL define an `IDownloadResponse` marker interface implemented by all download response messages.

#### Scenario: Marker interface implementations
- **WHEN** download response types are inspected
- **THEN** `DownloadAdded`, `QueueResult`, `HistoryResult`, `DeleteDownloadResult`, and `RetryDownloadResult` SHALL implement `IDownloadResponse`
