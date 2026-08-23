## Purpose

Clean REST API endpoints for querying download queue state and history, independent of the SABnzbd envelope format.

## Requirements

### Requirement: Clean queue endpoint
The system SHALL expose `GET /api/v1/queue` returning active downloads as a flat JSON array. Each job SHALL contain: nzoId, title, status (Queued/Downloading/Muxing), progressPercent, downloadedBytes, totalBytes, enqueuedAt. Progress data (progressPercent, downloadedBytes, totalBytes) SHALL be read from the shared `DownloadProgress` object associated with each job, not from `DownloadJob` fields. If no progress data is available for a job, progress fields SHALL default to 0.

#### Scenario: Active downloads with progress
- **WHEN** there are 3 active downloads, 2 with progress data and 1 without
- **THEN** the response SHALL be a JSON array with progress fields populated from the shared `DownloadProgress` objects for the 2 jobs that have them, and 0 for the job without

#### Scenario: Empty queue
- **WHEN** no downloads are active
- **THEN** the response SHALL return an empty array

### Requirement: Clean history endpoint
The system SHALL expose `GET /api/v1/history` returning completed and failed downloads as a flat JSON array, sorted by completion time descending.

#### Scenario: History with items
- **WHEN** there are completed and failed downloads
- **THEN** the response SHALL be a JSON array with each job containing: nzoId, title, status (Completed/Failed), outputPath, errorMessage, enqueuedAt, completedAt

#### Scenario: Path mapping applied
- **WHEN** a path mapping is configured
- **THEN** the outputPath in history items SHALL have the mapping applied

### Requirement: API key authentication
Both queue API endpoints SHALL require a valid `apikey` query parameter. Authentication SHALL be handled by the centralized `ApiKeyMiddleware`.

#### Scenario: Unauthenticated request
- **WHEN** a request has no apikey
- **THEN** the `ApiKeyMiddleware` SHALL return 401

### Requirement: Controller-based implementation
The queue endpoints SHALL be implemented as an MVC controller (`QueueController`) in the `FunkArr.Api` namespace with route prefix `/api/v1`. The controller SHALL use constructor injection for `ActorRegistry` and options.

#### Scenario: Versioned route
- **WHEN** a client sends `GET /api/v1/queue?apikey=key`
- **THEN** the system SHALL route to `QueueController`

### Requirement: Typed response models
Queue and history responses SHALL use typed DTOs (`QueueItemResponse`, `HistoryItemResponse`) instead of anonymous objects, enabling OpenAPI schema generation.

#### Scenario: OpenAPI schema available
- **WHEN** the OpenAPI spec is generated
- **THEN** the queue and history response schemas SHALL include all properties with their types

### Requirement: Queue response merges state and progress
The `QueueController` and `SabnzbdController` SHALL merge job state from the `DownloadQueueActor` response with progress data from the `DownloadProgress` objects. The actor's `QueueResponse` SHALL include the `DownloadProgress` reference for each active job alongside the `DownloadJob`.

#### Scenario: Controller reads progress without actor involvement
- **WHEN** the controller receives a `QueueResponse` with jobs and their associated progress objects
- **THEN** it reads `DownloadedBytes` and `TotalBytes` directly from the progress objects, computes `ProgressPercent`, and includes them in the response -- no additional actor message is needed
