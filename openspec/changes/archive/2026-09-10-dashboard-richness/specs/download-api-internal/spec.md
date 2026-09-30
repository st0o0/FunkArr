## ADDED Requirements

### Requirement: History stats endpoint
The system SHALL respond to `GET /api/downloads/history/stats` with aggregate statistics by sending `QueryHistoryStats` to the DownloadHistoryManager.

#### Scenario: Successful response
- **WHEN** `GET /api/downloads/history/stats` is requested
- **THEN** the response SHALL be JSON with `totalCompleted`, `totalFailed`, `totalBytes`, `averageDownloadTimeSeconds`, `successRate`

#### Scenario: Actor timeout
- **WHEN** the DownloadHistoryManager does not respond within the ask timeout
- **THEN** the response SHALL be HTTP 504 Gateway Timeout

### Requirement: History categories endpoint
The system SHALL respond to `GET /api/downloads/history/categories` with distinct category values by sending `QueryHistoryCategories` to the DownloadHistoryManager.

#### Scenario: Successful response
- **WHEN** `GET /api/downloads/history/categories` is requested
- **THEN** the response SHALL be a JSON array of unique category strings

#### Scenario: Actor timeout
- **WHEN** the DownloadHistoryManager does not respond within the ask timeout
- **THEN** the response SHALL be HTTP 504 Gateway Timeout

### Requirement: Storage status endpoint
The system SHALL respond to `GET /api/health/storage` with disk space information using `DriveInfo` for the configured complete and incomplete directory paths.

#### Scenario: Successful response
- **WHEN** `GET /api/health/storage` is requested
- **THEN** the response SHALL be JSON with `completeDirectory` and `incompleteDirectory` objects containing `path`, `availableBytes`, `totalBytes`

### Requirement: Cache stats endpoint
The system SHALL respond to `GET /api/health/cache` with TMDB/TVDB cache statistics by sending `QueryCacheStats` to the MetadataResolverManager.

#### Scenario: Successful response
- **WHEN** `GET /api/health/cache` is requested
- **THEN** the response SHALL be JSON with `tvdbEntries`, `tmdbEntries`, `oldestEntry`

#### Scenario: Actor timeout
- **WHEN** the MetadataResolverManager does not respond within the ask timeout
- **THEN** the response SHALL be HTTP 504 Gateway Timeout

## MODIFIED Requirements

### Requirement: Queue snapshot endpoint
The system SHALL respond to `GET /api/downloads/queue` with a JSON array of current queue items including progress data.

#### Scenario: Queue with items
- **WHEN** `GET /api/downloads/queue` is requested
- **THEN** the response SHALL be JSON with `items` array, `totalSlots` count, `activeCount` (int), and `queuedCount` (int)
- **AND** each item SHALL contain `downloadId` (string), `title` (string), `status` ("Queued" or "Processing"), `channel` (string), `category` (string), `totalBytes` (number), `bytesDownloaded` (number), `percentage` (0-100), `speed` (bytes/second), `eta` (formatted HH:MM:SS string)

#### Scenario: Empty queue
- **WHEN** `GET /api/downloads/queue` is requested and no downloads are queued or active
- **THEN** the response SHALL be JSON `{"items":[],"totalSlots":0,"activeCount":0,"queuedCount":0}`
