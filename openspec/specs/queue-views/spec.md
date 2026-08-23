## Purpose

UI views for displaying the active download queue with progress and the download history with completion/failure status.

## Requirements

### Requirement: Download queue view
The UI SHALL display all active downloads (status Queued, Downloading, or Muxing) with title, status, and progress information.

#### Scenario: Show active downloads
- **WHEN** there are 3 active downloads (1 downloading, 1 muxing, 1 queued)
- **THEN** the queue view SHALL display all 3 with their respective status labels and a progress bar for the downloading item

#### Scenario: Empty queue
- **WHEN** no downloads are active
- **THEN** the queue view SHALL display an empty state message

#### Scenario: Download progress
- **WHEN** a download is in progress with 142 MB of 212 MB downloaded
- **THEN** the view SHALL show a progress bar at 67%, the downloaded/total bytes, and the title

### Requirement: Queue auto-refresh
The queue view SHALL poll the queue API at a regular interval to update download progress.

#### Scenario: Polling active
- **WHEN** the queue view is visible
- **THEN** the UI SHALL poll `GET /api/v1/queue` every 3 seconds

#### Scenario: Polling paused
- **WHEN** the user navigates away from the queue view or the browser tab is hidden
- **THEN** polling SHALL pause until the view is visible again

### Requirement: Download history view
The UI SHALL display completed and failed downloads with title, status, completion time, and error messages for failures.

#### Scenario: Show history
- **WHEN** there are 5 completed and 2 failed downloads
- **THEN** the history view SHALL display all 7, sorted by completion time descending, with failed items showing error messages

#### Scenario: Empty history
- **WHEN** no downloads have completed or failed
- **THEN** the history view SHALL display an empty state message
