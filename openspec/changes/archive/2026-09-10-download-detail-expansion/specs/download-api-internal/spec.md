## MODIFIED Requirements

### Requirement: Queue snapshot endpoint
The system SHALL respond to `GET /api/downloads/queue` with a JSON array of current queue items including progress data.

#### Scenario: Queue with items
- **WHEN** `GET /api/downloads/queue` is requested
- **THEN** the response SHALL be JSON with `items` array and `totalSlots` count
- **AND** each item SHALL contain `downloadId` (string), `title` (string), `status` ("Queued" or "Processing"), `channel` (string), `category` (string), `totalBytes` (number), `bytesDownloaded` (number), `percentage` (0-100), `speed` (bytes/second), `eta` (formatted HH:MM:SS string), `hasSubtitles` (bool), `totalDuration` (int, seconds)

#### Scenario: Empty queue
- **WHEN** `GET /api/downloads/queue` is requested and no downloads are queued or active
- **THEN** the response SHALL be JSON `{"items":[],"totalSlots":0}`

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
