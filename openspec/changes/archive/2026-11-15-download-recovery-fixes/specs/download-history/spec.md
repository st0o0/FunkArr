## MODIFIED Requirements

### Requirement: Download history distinguishes Cancelled from Failed
The download history SHALL record cancelled downloads with a Cancelled status distinct from Failed. A download is cancelled when the user explicitly deletes it from the queue while it is active.

#### Scenario: Active download deleted by user
- **WHEN** a user deletes an active download via the UI or API
- **AND** the download worker receives CancelDownload
- **AND** the FfmpegResult indicates cancellation
- **THEN** the history entry SHALL record status Cancelled (not Failed)

#### Scenario: Download fails due to transient or permanent error
- **WHEN** a download fails due to an FFmpeg error (not user cancellation)
- **THEN** the history entry SHALL record status Failed (unchanged behavior)

#### Scenario: Download completes successfully
- **WHEN** a download completes without error
- **THEN** the history entry SHALL record status Completed (unchanged behavior)

#### Scenario: Persistence enum value
- **WHEN** the PersistedDownloadStatus enum is inspected
- **THEN** it SHALL contain Cancelled with an explicit int value
- **AND** existing Completed and Failed values SHALL be unchanged
