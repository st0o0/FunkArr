## ADDED Requirements

### Requirement: DownloadWorker handles StartDownload after recovery from Failed phase
When a DownloadWorker recovers in Failed status (e.g., after crashing during retry-wait) and receives a StartDownload from the manager's re-dispatch, it SHALL reset to Initialized and proceed with the download rather than silently dropping the message and leaking the manager slot.

#### Scenario: Re-dispatch after recovery from Failed phase
- **WHEN** a DownloadWorker recovers with status Failed (e.g., it crashed while waiting for a retry timer)
- **AND** the DownloadManager re-dispatches by sending StartDownload
- **THEN** the Worker SHALL persist a DownloadInitialized event to reset to Initialized status
- **AND** proceed with the normal StartDownload flow (persist DownloadAttemptStarted, start FFmpeg)
- **AND** the manager slot SHALL NOT be leaked

#### Scenario: Re-dispatch after recovery from Completed phase
- **WHEN** a DownloadWorker recovers with status Completed
- **AND** the DownloadManager re-dispatches by sending StartDownload
- **THEN** the Worker SHALL send SlotFree to the Manager to release the slot
- **AND** NOT start a new download

#### Scenario: Normal StartDownload on Initialized worker unchanged
- **WHEN** a DownloadWorker is in Initialized status
- **AND** receives StartDownload
- **THEN** the existing behavior SHALL be unchanged (persist DownloadAttemptStarted, start FFmpeg)

#### Scenario: StartDownload on uninitialized worker unchanged
- **WHEN** a DownloadWorker has not been initialized (no persisted state)
- **AND** receives StartDownload
- **THEN** the Worker SHALL ignore the message (existing behavior unchanged)
