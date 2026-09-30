# Download Messages (Delta)

## MODIFIED Requirements

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

### Requirement: QueryQueue query
The system SHALL define a `QueryQueue` record for requesting the current download queue state with optional pagination and category filter.

#### Scenario: QueryQueue fields
- **WHEN** a QueryQueue message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (string?, nullable, default null)

### Requirement: QueryHistory query
The system SHALL define a `QueryHistory` record for requesting download history with optional pagination and category filter.

#### Scenario: QueryHistory fields
- **WHEN** a QueryHistory message is created
- **THEN** it SHALL contain `Start` (int, default 0), `Limit` (int, default 0 meaning unlimited), `Category` (string?, nullable, default null)

### Requirement: DeleteDownload command
The system SHALL define a `DeleteDownload` record for removing an item from queue or history, with an optional flag to delete associated files.

#### Scenario: DeleteDownload fields
- **WHEN** a DeleteDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid) and `DeleteFiles` (bool, default false)
- **AND** it SHALL implement `IWithDownloadId`
