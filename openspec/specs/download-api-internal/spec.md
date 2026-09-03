# Download API Internal

## Purpose

Internal REST + SSE API endpoints for the FunkArr UI to query download queue state, download history, and perform actions (cancel, delete, retry). Lives in FunkArr.Api alongside existing RuleSet and Setup endpoints.

## Requirements

### Requirement: Queue snapshot endpoint
The system SHALL respond to `GET /api/downloads/queue` with a JSON array of current queue items including progress data.

#### Scenario: Queue with items
- **WHEN** `GET /api/downloads/queue` is requested
- **THEN** the response SHALL be JSON with `items` array and `totalSlots` count
- **AND** each item SHALL contain `downloadId` (string), `title` (string), `status` ("Queued" or "Processing"), `channel` (string), `category` (string), `totalBytes` (number), `bytesDownloaded` (number), `percentage` (0-100), `speed` (bytes/second), `eta` (formatted HH:MM:SS string)

#### Scenario: Empty queue
- **WHEN** `GET /api/downloads/queue` is requested and no downloads are queued or active
- **THEN** the response SHALL be JSON `{"items":[],"totalSlots":0}`

### Requirement: Queue SSE stream endpoint
The system SHALL respond to `GET /api/downloads/queue/stream` with a `text/event-stream` response that pushes the full queue state at a regular interval.

#### Scenario: SSE connection established
- **WHEN** a client connects to `GET /api/downloads/queue/stream`
- **THEN** the response content type SHALL be `text/event-stream`
- **AND** the response SHALL disable buffering (`Cache-Control: no-cache`)

#### Scenario: Periodic queue events
- **WHEN** a client is connected to the SSE stream
- **THEN** the server SHALL send an event with `event: queue` and `data:` containing the same JSON structure as the queue snapshot endpoint
- **AND** events SHALL be sent approximately every 3 seconds

#### Scenario: Client disconnection
- **WHEN** the client closes the SSE connection
- **THEN** the server SHALL stop the polling loop and release resources

#### Scenario: Actor query failure during SSE
- **WHEN** the DownloadManager does not respond within the ask timeout during an SSE tick
- **THEN** the server SHALL skip that tick and retry on the next interval
- **AND** the SSE connection SHALL remain open

### Requirement: History endpoint with pagination
The system SHALL respond to `GET /api/downloads/history` with a paginated JSON list of completed and failed download records.

#### Scenario: History with items
- **WHEN** `GET /api/downloads/history` is requested
- **THEN** the response SHALL be JSON with `items` array and `totalItems` count
- **AND** each item SHALL contain `downloadId` (string), `title` (string), `category` (string), `totalBytes` (number), `downloadTimeSeconds` (number), `filePath` (string or null), `status` ("Completed" or "Failed"), `failMessage` (string or null), `completedAt` (ISO 8601 string)

#### Scenario: Pagination parameters
- **WHEN** `GET /api/downloads/history?start=10&limit=25` is requested
- **THEN** the response SHALL contain at most 25 items starting from index 10
- **AND** `totalItems` SHALL reflect the total count before pagination

#### Scenario: Default pagination
- **WHEN** `GET /api/downloads/history` is requested without `start` or `limit`
- **THEN** the system SHALL default to `start=0` and `limit=25`

#### Scenario: Category filter
- **WHEN** `GET /api/downloads/history?category=sonarr` is requested
- **THEN** the response SHALL contain only items matching category "sonarr"

#### Scenario: Empty history
- **WHEN** no downloads have completed or failed
- **THEN** the response SHALL be JSON `{"items":[],"totalItems":0}`

### Requirement: Delete queue item endpoint
The system SHALL respond to `DELETE /api/downloads/queue/{id}` by sending a `DeleteDownload` message to the DownloadManager.

#### Scenario: Successful deletion
- **WHEN** `DELETE /api/downloads/queue/{id}` is requested with a valid DownloadId
- **THEN** the system SHALL send `DeleteDownload` to the Manager
- **AND** respond with HTTP 200 and JSON `{"success":true}`

#### Scenario: Item not found
- **WHEN** `DELETE /api/downloads/queue/{id}` is requested for a DownloadId not in the queue
- **THEN** the response SHALL be HTTP 404 with JSON `{"success":false,"error":"Item not found"}`

#### Scenario: Invalid GUID
- **WHEN** `DELETE /api/downloads/queue/{id}` is requested with a non-GUID string
- **THEN** the response SHALL be HTTP 400

### Requirement: Delete history item endpoint
The system SHALL respond to `DELETE /api/downloads/history/{id}` by sending a `RemoveHistoryEntry` message to the DownloadHistoryActor.

#### Scenario: Successful deletion
- **WHEN** `DELETE /api/downloads/history/{id}` is requested with a valid DownloadId
- **THEN** the system SHALL send `RemoveHistoryEntry` to the HistoryActor
- **AND** respond with HTTP 200 and JSON `{"success":true}`

#### Scenario: Item not found
- **WHEN** `DELETE /api/downloads/history/{id}` is requested for a DownloadId not in the history
- **THEN** the response SHALL be HTTP 404 with JSON `{"success":false,"error":"Item not found"}`

### Requirement: Retry failed download endpoint
The system SHALL respond to `POST /api/downloads/{id}/retry` by sending a `RetryDownload` message to the DownloadManager and removing the history entry.

#### Scenario: Successful retry
- **WHEN** `POST /api/downloads/{id}/retry` is requested for a failed download
- **THEN** the system SHALL send `RemoveHistoryEntry` to the HistoryActor
- **AND** send `RetryDownload` to the Manager
- **AND** respond with HTTP 200 and JSON `{"success":true}`

#### Scenario: Retry failure
- **WHEN** `POST /api/downloads/{id}/retry` is requested and the Manager returns failure
- **THEN** the response SHALL be HTTP 400 with JSON `{"success":false,"error":"<message>"}`

### Requirement: API response models
The internal API SHALL use its own response model records in `FunkArr.Api/Models/`, decoupled from the actor message types. The API layer SHALL compute derived fields (percentage, speed, ETA) from the raw actor data.

#### Scenario: Queue item percentage calculation
- **WHEN** a queue item has `CurrentTimeUs` and `TotalDuration`
- **THEN** `percentage` SHALL be calculated as `(CurrentTimeUs / 1_000_000) / TotalDuration * 100`, clamped to 0-100

#### Scenario: Queue item speed calculation
- **WHEN** a queue item has `BytesDownloaded` and `CurrentTimeUs > 0`
- **THEN** `speed` SHALL be calculated as `BytesDownloaded / (CurrentTimeUs / 1_000_000)` in bytes/second

#### Scenario: Queue item ETA calculation
- **WHEN** a queue item has speed > 0 and remaining bytes > 0
- **THEN** `eta` SHALL be formatted as `HH:MM:SS` based on remaining bytes at current speed

#### Scenario: Queue item with no progress
- **WHEN** a queue item has status Queued or no progress data yet
- **THEN** `percentage` SHALL be 0, `speed` SHALL be 0, `eta` SHALL be "00:00:00"

### Requirement: Endpoint registration
The download API endpoints SHALL be registered via a `MapDownloadInternalApi` extension method on `WebApplication`, called from `ApplicationSetupContainer`.

#### Scenario: Endpoint group path
- **WHEN** the download internal API is registered
- **THEN** all endpoints SHALL be under the `/api/downloads` path prefix
