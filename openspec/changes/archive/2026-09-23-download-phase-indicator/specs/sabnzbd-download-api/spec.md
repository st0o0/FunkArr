## MODIFIED Requirements

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` by delegating to `SabnzbdQueueService` which queries the DownloadManager actor and translates the response to SABnzbd JSON format via `SabnzbdResponseMapper`. The controller SHALL NOT contain inline response mapping logic. It SHALL accept optional `start` (int), `limit` (int), `category` (string), and `name` (string, subcommand) parameters. The `percentage` field SHALL use phase-aware calculation based on whether the download is in the downloading or remuxing phase.

#### Scenario: Queue with active downloads
- **WHEN** the DownloadManager has items in Queued or Processing status
- **THEN** `SabnzbdQueueService` SHALL query the actor and `SabnzbdResponseMapper` SHALL build each slot with `nzo_id`, `status`, `filename`, `cat`, `mb`, `mbleft`, `percentage`, `timeleft`, `speed`, `priority`, `index`

#### Scenario: Queue progress mapping
- **WHEN** a queue item has DownloadStatus Processing with progress data
- **THEN** `percentage` SHALL be phase-aware: bytes-based (`BytesDownloaded * 100 / TotalBytes`) during downloading, time-based (`(CurrentTimeUs / 1_000_000) / TotalDuration * 100`) during remuxing
- **AND** `mbleft` SHALL be calculated as `(TotalBytes - BytesDownloaded) / 1_048_576`
- **AND** `timeleft` SHALL be formatted as `HH:MM:SS` based on remaining time at current speed
- **AND** `status` SHALL be `"Downloading"` for both phases to maintain Sonarr/Radarr compatibility

#### Scenario: Queue item with no progress yet
- **WHEN** a queue item has DownloadStatus Processing but no progress data received yet
- **THEN** `percentage` SHALL be `"0"`, `mbleft` SHALL equal `mb`, `timeleft` SHALL be `"00:00:00"`, and `speed` SHALL be `"0"`

#### Scenario: Empty queue
- **WHEN** no downloads are in Queued or Processing status
- **THEN** the response SHALL be JSON with `queue.slots` as empty array and `queue.noofslots_total` as 0

#### Scenario: Queue category filter
- **WHEN** `?mode=queue&category=sonarr` is requested
- **THEN** the response SHALL contain only queue slots matching category "sonarr"
