# Download Messages (Delta)

## MODIFIED Requirements

### Requirement: AddDownload command
The system SHALL define an `AddDownload` record containing all metadata needed to start a download, extracted from the NZB file. The Title SHALL be the scene-style formatted release title.

#### Scenario: AddDownload fields
- **WHEN** an AddDownload message is created
- **THEN** it SHALL contain `Title` (string, scene-formatted), `VideoUrl` (string), `SubtitleUrl` (string?, nullable), `Channel` (string), `Duration` (int, seconds), `Size` (long, bytes), `Category` (MediaType), `Priority` (DownloadPriority, default Normal)

#### Scenario: Title is scene-formatted
- **WHEN** an AddDownload is created from a parsed NZB
- **THEN** the Title SHALL already be a scene-style string (e.g. `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`)
- **AND** the DownloadManager SHALL use this title directly for the output filename by appending `.mkv`

### Requirement: QueueResult response
The system SHALL define a `QueueResult` record containing the current queue state with pagination metadata.

#### Scenario: QueueResult fields
- **WHEN** a QueueResult message is created
- **THEN** it SHALL contain `Items` (array of `QueueItem`), `TotalSlots` (int), and `TotalItems` (int)

#### Scenario: QueueItem fields
- **WHEN** a QueueItem is inspected
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Status` (DownloadStatus), `TotalBytes` (long), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `Category` (MediaType), `Channel` (string), `HasSubtitles` (bool), `Priority` (DownloadPriority)

### Requirement: DownloadEnqueued persistence DTO
The system SHALL define a `DownloadEnqueued` persistence DTO for the Manager's queue add event.

#### Scenario: DownloadEnqueued fields
- **WHEN** a `DownloadEnqueued` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid) and `Priority` (DownloadPriority)
