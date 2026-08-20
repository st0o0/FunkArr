## Purpose

Clean REST API endpoints for querying download queue state and history, independent of the SABnzbd envelope format.

## Requirements

### Requirement: Clean queue endpoint
The system SHALL expose `GET /api/v1/queue` returning active downloads as a flat JSON array (not wrapped in SABnzbd envelope format).

#### Scenario: Active downloads
- **WHEN** there are 3 active downloads
- **THEN** the response SHALL be a JSON array with each job containing: nzoId, title, status (Queued/Downloading/Muxing), progressPercent, downloadedBytes, totalBytes, enqueuedAt

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
