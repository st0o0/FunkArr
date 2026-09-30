# Download Messages

## MODIFIED Requirements

### Requirement: InitDownload command
The system SHALL define an `InitDownload` record sent from Manager to Worker to initialize the Worker with all download metadata.

#### Scenario: InitDownload fields
- **WHEN** an InitDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (string), `IncompletePath` (string), `OutputPath` (string)
- **AND** it SHALL implement `IWithDownloadId`
