## MODIFIED Requirements

### Requirement: Clean queue endpoint
The system SHALL expose `GET /api/v1/queue` returning active downloads as a flat JSON array. Each job SHALL contain: nzoId, title, status (Queued/Downloading/Muxing), category, progressPercent, downloadedBytes, totalBytes, enqueuedAt. Progress data (progressPercent, downloadedBytes, totalBytes) SHALL be read from the shared `DownloadProgress` object associated with each job, not from `DownloadJob` fields. If no progress data is available for a job, progress fields SHALL default to 0.

#### Scenario: Active downloads with category
- **WHEN** there are active downloads with categories
- **THEN** the response SHALL include a `category` field (nullable string) for each item

### Requirement: Clean history endpoint
The system SHALL expose `GET /api/v1/history` returning completed and failed downloads as a flat JSON array, sorted by completion time descending.

#### Scenario: History with category
- **WHEN** there are completed downloads with categories
- **THEN** the response SHALL include a `category` field (nullable string) for each item alongside nzoId, title, status, outputPath, errorMessage, enqueuedAt, completedAt
