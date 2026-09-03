## MODIFIED Requirements

### Requirement: InitDownload command
The system SHALL define an `InitDownload` record sent from Manager to Worker to initialize the Worker with all download metadata. Infrastructure paths (IncompletePath, OutputPath) SHALL NOT be included — the Worker computes these at runtime.

#### Scenario: InitDownload fields
- **WHEN** an InitDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (string)
- **AND** it SHALL implement `IWithDownloadId`
- **AND** it SHALL NOT contain `IncompletePath` or `OutputPath`

### Requirement: DownloadStarted persistence DTO
The system SHALL define a `DownloadInitialized` persistence DTO in `FunkArr.Persistence.Events.Download` for the Worker's initialization event. Infrastructure paths SHALL NOT be persisted.

#### Scenario: DownloadInitialized fields
- **WHEN** a DownloadInitialized event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (string)
- **AND** it SHALL NOT contain `IncompletePath` or `OutputPath`
