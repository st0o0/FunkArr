## MODIFIED Requirements

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` by delegating to `SabnzbdQueueService` which queries the DownloadManager actor and translates the response to SABnzbd JSON format via `SabnzbdResponseMapper`. The controller SHALL NOT contain inline response mapping logic.

#### Scenario: Queue with active downloads
- **WHEN** the DownloadManager has items in Queued or Processing status
- **THEN** `SabnzbdQueueService` SHALL query the actor and `SabnzbdResponseMapper` SHALL build each slot with `nzo_id`, `status`, `filename`, `cat`, `mb`, `mbleft`, `percentage`, `timeleft`, `speed`, `priority`, `index`

#### Scenario: Empty queue
- **WHEN** no downloads are in Queued or Processing status
- **THEN** the response SHALL be JSON with `queue.slots` as empty array and `queue.noofslots_total` as 0

#### Scenario: Queue category filter
- **WHEN** `?mode=queue&category=sonarr` is requested
- **THEN** the response SHALL contain only queue slots matching category "sonarr"

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` by delegating to `SabnzbdQueueService` which queries the DownloadHistoryManager actor and translates the response to SABnzbd JSON format via `SabnzbdResponseMapper`.

#### Scenario: History with completed downloads
- **WHEN** the DownloadHistoryManager has items in history
- **THEN** `SabnzbdQueueService` SHALL query the actor and `SabnzbdResponseMapper` SHALL build each slot with `nzo_id`, `name`, `nzb_name`, `category`, `bytes`, `download_time`, `storage`, `status`, `fail_message`, `completed_on`

#### Scenario: Empty history
- **WHEN** no downloads have completed or failed
- **THEN** the response SHALL be JSON `{"history":{"noofslots":0,"slots":[]}}`

### Requirement: Full status endpoint
The system SHALL respond to `GET /download/api?mode=fullstatus` by delegating to `SabnzbdQueueService` which queries the DownloadManager actor for speed and status.

#### Scenario: Full status response
- **WHEN** `?mode=fullstatus` is requested
- **THEN** `SabnzbdQueueService` SHALL return the status object via the controller

### Requirement: Add file endpoint
The system SHALL respond to `POST /download/api?mode=addfile` by delegating to `SabnzbdDownloadService` which parses the NZB via `NzbService`, extracts metadata, and sends `AddDownload` to the DownloadManager actor.

#### Scenario: Successful addfile
- **WHEN** a valid NZB is POSTed as multipart/form-data
- **THEN** `SabnzbdDownloadService` SHALL use `NzbService` for parsing, extract metadata, send `AddDownload`, and return `{"status":true,"nzo_ids":["<id>"]}`

#### Scenario: Missing NZB file
- **WHEN** no `nzbfile` form field is provided
- **THEN** the response SHALL be `{"status":false,"error":"No NZB file uploaded"}` with HTTP 400

### Requirement: Queue delete subcommand
The system SHALL respond to `GET /download/api?mode=queue&name=delete&value=<nzo_id>` by delegating to `SabnzbdDownloadService` which sends `DeleteDownload` to the DownloadManager actor.

#### Scenario: Successful queue item deletion
- **WHEN** `?mode=queue&name=delete&value=existing-id` is requested
- **THEN** `SabnzbdDownloadService` SHALL send DeleteDownload and return `{"status":true}`

#### Scenario: Queue delete non-existent item
- **WHEN** `?mode=queue&name=delete&value=non-existent-id` is requested
- **THEN** the response SHALL be `{"status":false,"error":"Item not found"}`

### Requirement: Retry failed download
The system SHALL respond to `GET /download/api?mode=retry&value=<nzo_id>` by delegating to `SabnzbdDownloadService` which sends `RetryDownload` to the DownloadManager actor.

#### Scenario: Successful retry
- **WHEN** `?mode=retry&value=failed-item-id` is requested
- **THEN** `SabnzbdDownloadService` SHALL send RetryDownload and return `{"status":true}`

### Requirement: Delete history item
The system SHALL respond to `GET /download/api?mode=history&name=delete&value=<nzo_id>` by delegating to `SabnzbdDownloadService` which sends `RemoveHistoryEntry` to the DownloadHistoryManager actor.

#### Scenario: Successful history deletion
- **WHEN** `?mode=history&name=delete&value=existing-id` is requested
- **THEN** `SabnzbdDownloadService` SHALL send RemoveHistoryEntry and return `{"status":true}`
