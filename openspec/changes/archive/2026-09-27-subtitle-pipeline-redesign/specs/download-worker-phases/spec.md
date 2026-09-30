## REMOVED Requirements

### Requirement: DownloadPhase.SubtitleDownload phase tracking
**Reason**: The `SubtitleDownload` phase value is dead code -- never set by any actor. Subtitle download occurs inside `Remuxer.RunAsync` during the `Remux` phase and completes in under a second. Adding subtitle telemetry (counter + duration histogram) provides better visibility than a phase enum value.
**Migration**: Remove `SubtitleDownload = 1` from the `DownloadPhase` enum. Renumber remaining values: `Initialized` (0), `VideoDownload` (1), `Remuxing` (2), `Moving` (3), `Completed` (4), `Failed` (5). Update `CalculatePercentage` and `IsTransient` to remove `SubtitleDownload` handling.

## MODIFIED Requirements

### Requirement: DownloadPhase enum values
The `DownloadPhase` enum in `FunkArr.Messages.Download` SHALL be updated to remove the unused `SubtitleDownload` value.

#### Scenario: Enum values
- **WHEN** the DownloadPhase enum is inspected
- **THEN** it SHALL contain `Initialized` (0), `VideoDownload` (1), `Remuxing` (2), `Moving` (3), `Completed` (4), `Failed` (5)
- **AND** `SubtitleDownload` SHALL NOT exist
