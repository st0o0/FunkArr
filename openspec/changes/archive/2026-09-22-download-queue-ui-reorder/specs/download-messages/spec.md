# Download Messages (Delta)

## MODIFIED Requirements

### Requirement: MoveDownload command
The system SHALL define a `MoveDownload(Guid DownloadId, int Position, DownloadPriority? Priority = null)` command in `FunkArr.Messages.Download`. It SHALL implement `IWithDownloadId`. The optional `Priority` parameter enables atomic cross-bucket moves.

#### Scenario: MoveDownload fields
- **WHEN** a MoveDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Position` (int), and `Priority` (DownloadPriority?, default null)
