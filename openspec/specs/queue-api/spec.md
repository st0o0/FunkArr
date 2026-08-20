## Purpose

Clean REST API endpoints for querying download queue state and history, independent of the SABnzbd envelope format.

## Requirements

### Requirement: Clean queue endpoint
The system SHALL expose `GET /api/queue` returning active downloads as a flat JSON array (not wrapped in SABnzbd envelope format).

#### Scenario: Active downloads
- **WHEN** there are 3 active downloads
- **THEN** the response SHALL be a JSON array with each job containing: nzoId, title, status (Queued/Downloading/Muxing), progressPercent, downloadedBytes, totalBytes, enqueuedAt

#### Scenario: Empty queue
- **WHEN** no downloads are active
- **THEN** the response SHALL return an empty array

### Requirement: Clean history endpoint
The system SHALL expose `GET /api/history` returning completed and failed downloads as a flat JSON array, sorted by completion time descending.

#### Scenario: History with items
- **WHEN** there are completed and failed downloads
- **THEN** the response SHALL be a JSON array with each job containing: nzoId, title, status (Completed/Failed), outputPath, errorMessage, enqueuedAt, completedAt

#### Scenario: Path mapping applied
- **WHEN** a path mapping is configured
- **THEN** the outputPath in history items SHALL have the mapping applied

### Requirement: API key authentication
Both queue API endpoints SHALL require a valid `apikey` query parameter.

#### Scenario: Unauthenticated request
- **WHEN** a request has no apikey
- **THEN** the response SHALL be 401
