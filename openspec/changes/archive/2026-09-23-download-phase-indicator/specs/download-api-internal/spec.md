## MODIFIED Requirements

### Requirement: API response models
The internal API SHALL use its own response model records in `FunkArr.Api/Models/`, decoupled from the actor message types. The API layer SHALL compute derived fields (percentage, phase, speed, ETA) from the raw actor data. The `DownloadQueueItem` SHALL include a `phase` field of type string with value `"downloading"` or `"remuxing"`.

#### Scenario: Queue item percentage calculation
- **WHEN** a queue item is in the downloading phase (`BytesDownloaded < TotalBytes`, or `BytesDownloaded >= TotalBytes` and `CurrentTimeUs == 0`)
- **THEN** `percentage` SHALL be calculated as `BytesDownloaded * 100 / TotalBytes`, clamped to 0-100
- **WHEN** a queue item is in the remuxing phase (`BytesDownloaded >= TotalBytes` and `CurrentTimeUs > 0`)
- **THEN** `percentage` SHALL be calculated as `(CurrentTimeUs / 1_000_000) / TotalDuration * 100`, clamped to 0-100

#### Scenario: Phase field present in queue response
- **WHEN** the client requests `GET /api/downloads/queue`
- **THEN** each queue item SHALL include a `phase` field with value `"downloading"` or `"remuxing"`

#### Scenario: Queue item speed calculation
- **WHEN** a queue item has `BytesDownloaded` and `CurrentTimeUs > 0`
- **THEN** `speed` SHALL be calculated as `BytesDownloaded / (CurrentTimeUs / 1_000_000)` in bytes/second

#### Scenario: Queue item ETA calculation
- **WHEN** a queue item has speed > 0 and remaining bytes > 0
- **THEN** `eta` SHALL be formatted as `HH:MM:SS` based on remaining bytes at current speed

#### Scenario: Queue item with no progress
- **WHEN** a queue item has status Queued or no progress data yet
- **THEN** `percentage` SHALL be 0, `speed` SHALL be 0, `eta` SHALL be "00:00:00"
