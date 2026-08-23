## MODIFIED Requirements

### Requirement: Clean queue endpoint
The system SHALL expose `GET /api/v1/queue` returning active downloads as a flat JSON array. Each job SHALL contain: nzoId, title, status (Queued/Downloading/Muxing), progressPercent, downloadedBytes, totalBytes, enqueuedAt. Progress data (progressPercent, downloadedBytes, totalBytes) SHALL be read from the shared `DownloadProgress` object associated with each job, not from `DownloadJob` fields. If no progress data is available for a job, progress fields SHALL default to 0.

#### Scenario: Active downloads with progress
- **WHEN** there are 3 active downloads, 2 with progress data and 1 without
- **THEN** the response SHALL be a JSON array with progress fields populated from the shared `DownloadProgress` objects for the 2 jobs that have them, and 0 for the job without

#### Scenario: Empty queue
- **WHEN** no downloads are active
- **THEN** the response SHALL return an empty array

### Requirement: Queue response merges state and progress
The `QueueController` and `SabnzbdController` SHALL merge job state from the `DownloadQueueActor` response with progress data from the `DownloadProgress` objects. The actor's `QueueResponse` SHALL include the `DownloadProgress` reference for each active job alongside the `DownloadJob`.

#### Scenario: Controller reads progress without actor involvement
- **WHEN** the controller receives a `QueueResponse` with jobs and their associated progress objects
- **THEN** it reads `DownloadedBytes` and `TotalBytes` directly from the progress objects, computes `ProgressPercent`, and includes them in the response — no additional actor message is needed
