## MODIFIED Requirements

### Requirement: Add file endpoint
The system SHALL respond to `POST /download/api?mode=addfile&cat=<category>` by accepting an NZB file as a multipart/form-data upload (field name `nzbfile`), parsing all metadata from the NZB XML, sending an `AddDownload` message to the DownloadManager, and returning the assigned download ID.

#### Scenario: Successful addfile via multipart
- **WHEN** a valid NZB is POSTed as multipart/form-data with field `nzbfile` and `?mode=addfile&cat=sonarr`
- **THEN** the system SHALL parse the NZB, extract VideoUrl, SubtitleUrl, Title, Channel, Duration, and Size from meta elements, send AddDownload to the DownloadManager, and respond with JSON `{"status":true,"nzo_ids":["<download-id>"]}`

#### Scenario: Missing NZB file
- **WHEN** a POST is made with `?mode=addfile` but no `nzbfile` form field
- **THEN** the response SHALL be JSON `{"status":false,"error":"No NZB file uploaded"}` with HTTP 400

#### Scenario: Invalid NZB format
- **WHEN** the uploaded NZB file does not contain a parseable `X-FunkArr-Url` meta element
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid NZB format"}` with HTTP 400

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` by querying the DownloadManager for current queue state and translating the response to SABnzbd JSON format.

#### Scenario: Queue with active downloads
- **WHEN** the DownloadManager has items in Queued or Processing status
- **THEN** each slot SHALL contain `nzo_id` (DownloadId string), `status` ("Queued" or "Downloading"), `filename` (title), `cat` (category), `mb` (total MB), `mbleft` (remaining MB), `percentage` (0-100), `timeleft` (formatted), `speed` (formatted), `priority` ("Normal"), `index` (position)

#### Scenario: Queue progress mapping
- **WHEN** a queue item has DownloadStatus Processing with progress data
- **THEN** `percentage` SHALL be calculated as `(CurrentTimeUs / 1_000_000) / TotalDuration * 100`
- **AND** `mbleft` SHALL be calculated as `(TotalBytes - BytesDownloaded) / 1_048_576`
- **AND** `timeleft` SHALL be formatted as `HH:MM:SS` based on remaining time at current speed
- **AND** `status` SHALL be `"Downloading"`

#### Scenario: Queue item with no progress yet
- **WHEN** a queue item has DownloadStatus Processing but no progress data received yet
- **THEN** `percentage` SHALL be `"0"`, `mbleft` SHALL equal `mb`, `timeleft` SHALL be `"00:00:00"`, and `speed` SHALL be `"0"`

#### Scenario: Empty queue
- **WHEN** no downloads are in Queued or Processing status
- **THEN** the response SHALL be JSON with `queue.slots` as empty array and `queue.noofslots_total` as 0

### Requirement: Queue delete subcommand
The system SHALL respond to `GET /download/api?mode=queue&name=delete&value=<nzo_id>` by sending a `DeleteDownload` message to the DownloadManager.

#### Scenario: Successful queue item deletion
- **WHEN** `?mode=queue&name=delete&value=existing-id` is requested
- **THEN** the system SHALL send DeleteDownload to the Manager, and respond with JSON `{"status":true}` on success

#### Scenario: Queue delete non-existent item
- **WHEN** `?mode=queue&name=delete&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` by querying the DownloadManager for history and translating the response to SABnzbd JSON format.

#### Scenario: History with completed downloads
- **WHEN** the DownloadManager has items in history
- **THEN** each slot SHALL contain `nzo_id` (DownloadId string), `name` (title), `nzb_name` (title + ".nzb"), `category` (category), `bytes` (total bytes), `download_time` (seconds), `storage` (file path), `status` ("Completed" or "Failed"), `fail_message` (error string or empty), `completed_on` (Unix timestamp)

#### Scenario: Empty history
- **WHEN** no downloads have completed or failed
- **THEN** the response SHALL be JSON `{"history":{"noofslots":0,"slots":[]}}`

### Requirement: Delete history item
The system SHALL respond to `GET /download/api?mode=history&name=delete&value=<nzo_id>` by sending a `DeleteDownload` message to the DownloadManager.

#### Scenario: Successful history deletion
- **WHEN** `?mode=history&name=delete&value=existing-id` is requested
- **THEN** the system SHALL send DeleteDownload to the Manager, and respond with JSON `{"status":true}` on success

#### Scenario: Delete non-existent history item
- **WHEN** `?mode=history&name=delete&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

### Requirement: Retry failed download
The system SHALL respond to `GET /download/api?mode=retry&value=<nzo_id>` by sending a `RetryDownload` message to the DownloadManager.

#### Scenario: Successful retry
- **WHEN** `?mode=retry&value=failed-item-id` is requested and the item exists in history with status Failed
- **THEN** the system SHALL send RetryDownload to the Manager, and respond with JSON `{"status":true}` on success

#### Scenario: Retry non-failed item
- **WHEN** `?mode=retry&value=completed-item-id` is requested and the item has status Completed
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item is not failed"}`

#### Scenario: Retry non-existent item
- **WHEN** `?mode=retry&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

### Requirement: Full status endpoint
The system SHALL respond to `GET /download/api?mode=fullstatus` with a JSON status object. The `speed` field SHALL reflect the aggregate download speed from all active downloads.

#### Scenario: Full status with active downloads
- **WHEN** `?mode=fullstatus` is requested and downloads are active
- **THEN** the response SHALL include `status.speed` as the sum of all active download speeds formatted as bytes/second string
