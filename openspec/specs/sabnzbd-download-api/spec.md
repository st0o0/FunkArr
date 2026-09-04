# SABnzbd Download API

## Purpose

SABnzbd-compatible download client API exposing version, config, full status, queue (with delete subcommand), history, addfile, retry, pagination, and delete endpoints for integration with Sonarr and Radarr.

## Requirements

### Requirement: Version endpoint
The system SHALL respond to `GET /download/api?mode=version` with a JSON object containing a SABnzbd version string.

#### Scenario: Version response
- **WHEN** `?mode=version` is requested
- **THEN** the response SHALL be JSON `{"version":"4.3.3"}`

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a JSON object containing SABnzbd configuration including complete_dir, categories, sorting settings, and sorters.

#### Scenario: Config response structure
- **WHEN** `?mode=get_config` is requested
- **THEN** the response SHALL be JSON with `config.misc.complete_dir` set to `DownloadOptions.CompletePath` (i.e., `{DownloadPath}/complete`), and `config.categories` dynamically built from `DownloadOptions.Categories`

#### Scenario: Config category entries
- **WHEN** the config is returned and `DownloadOptions.Categories` contains entries
- **THEN** each entry in `config.categories` SHALL contain `name` (from category Name), `order` (index), `dir` (resolved directory name), `newzbin` (empty string), `priority` (0)

#### Scenario: Config with no categories configured
- **WHEN** the config is returned and `DownloadOptions.Categories` is empty
- **THEN** `config.categories` SHALL be an empty array

#### Scenario: Config sorting disabled
- **WHEN** the config is returned
- **THEN** `config.misc.enable_tv_sorting`, `config.misc.enable_movie_sorting`, and `config.misc.enable_date_sorting` SHALL be `false`

#### Scenario: Config pre_check field
- **WHEN** the config is returned
- **THEN** `config.misc.pre_check` SHALL be `false`

#### Scenario: Config history retention
- **WHEN** the config is returned
- **THEN** `config.misc.history_retention` SHALL be `"all"`

#### Scenario: Config sorting category lists
- **WHEN** the config is returned
- **THEN** `config.misc` SHALL contain `tv_categories` (empty array), `movie_categories` (empty array), and `date_categories` (empty array)

#### Scenario: Config sorters empty
- **WHEN** the config is returned
- **THEN** `config.sorters` SHALL be an empty array

### Requirement: Full status endpoint
The system SHALL respond to `GET /download/api?mode=fullstatus` with a JSON status object. This endpoint is called by Sonarr/Radarr during connection testing.

#### Scenario: Full status response structure
- **WHEN** `?mode=fullstatus` is requested
- **THEN** the response SHALL be JSON with a `status` object containing `paused` (bool, default false), `speedlimit` (string, default ""), `diskspace1` (string, free GB), `diskspace2` (string, free GB), `completedir` (string, `DownloadOptions.CompletePath`), and `speed` (string, aggregate bytes/second of active downloads)

#### Scenario: Skip dashboard parameter accepted
- **WHEN** `?mode=fullstatus&skip_dashboard=1` is requested
- **THEN** the response SHALL be the same as without `skip_dashboard` (parameter accepted but ignored)

#### Scenario: Full status with active downloads
- **WHEN** `?mode=fullstatus` is requested and downloads are active
- **THEN** the response SHALL include `status.speed` as the sum of all active download speeds formatted as bytes/second string

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` by querying the DownloadManager for current queue state and translating the response to SABnzbd JSON format. It SHALL accept optional `start` (int), `limit` (int), `category` (string), and `name` (string, subcommand) parameters.

#### Scenario: Queue with active downloads
- **WHEN** the DownloadManager has items in Queued or Processing status
- **THEN** each slot SHALL contain `nzo_id` (DownloadId string), `status` ("Queued" or "Downloading"), `filename` (title), `cat` (category), `mb` (total MB), `mbleft` (remaining MB), `percentage` (0-100), `timeleft` (formatted), `speed` (bytes/second string), `priority` ("Normal"), `index` (position)
- **AND** the slot SHALL NOT contain a file path field

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
The system SHALL respond to `GET /download/api?mode=queue&name=delete&value=<nzo_id>` by sending a `DeleteDownload` message to the DownloadManager. It SHALL accept an optional `del_files` parameter.

#### Scenario: Successful queue item deletion
- **WHEN** `?mode=queue&name=delete&value=existing-id` is requested
- **THEN** the system SHALL send DeleteDownload to the Manager, and respond with JSON `{"status":true}` on success

#### Scenario: Queue delete with del_files
- **WHEN** `?mode=queue&name=delete&value=existing-id&del_files=1` is requested
- **THEN** the system SHALL send DeleteDownload with DeleteFiles=true to the Manager

#### Scenario: Queue delete non-existent item
- **WHEN** `?mode=queue&name=delete&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` by querying the DownloadManager for history and translating the response to SABnzbd JSON format. The `storage` field SHALL be derived by resolving `RelativePath` against `DownloadOptions.CompletePath` and extracting the directory. It SHALL accept optional `start` (int), `limit` (int), `category` (string), and `name` (string, subcommand) parameters.

#### Scenario: History with completed downloads
- **WHEN** the DownloadManager has items in history
- **THEN** each slot SHALL contain `nzo_id` (DownloadId string), `name` (title), `nzb_name` (title + ".nzb"), `category` (category), `bytes` (total bytes), `download_time` (seconds), `storage` (resolved directory path), `status` ("Completed", "Failed", "Extracting", "Moving", or "Verifying"), `fail_message` (error string or empty), `completed_on` (Unix timestamp)

#### Scenario: Completed download storage path
- **WHEN** the history endpoint builds a `HistorySlot` for a completed download
- **AND** the internal `RelativePath` is `"tv/Show.S01E01/Show.S01E01.mkv"`
- **THEN** the `storage` field SHALL be `"{CompletePath}/tv/Show.S01E01"` (directory of the resolved absolute path)

#### Scenario: Failed download storage path
- **WHEN** the history endpoint builds a `HistorySlot` for a failed download
- **AND** `RelativePath` is null or empty
- **THEN** the `storage` field SHALL be null

#### Scenario: Empty history
- **WHEN** no downloads have completed or failed
- **THEN** the response SHALL be JSON `{"history":{"noofslots":0,"slots":[]}}`

### Requirement: Delete history item
The system SHALL respond to `GET /download/api?mode=history&name=delete&value=<nzo_id>` by sending a `DeleteDownload` message to the DownloadManager. It SHALL accept optional `del_files` and `archive` parameters.

#### Scenario: Successful history deletion
- **WHEN** `?mode=history&name=delete&value=existing-id` is requested
- **THEN** the system SHALL send DeleteDownload to the Manager, and respond with JSON `{"status":true}` on success

#### Scenario: History delete with del_files
- **WHEN** `?mode=history&name=delete&value=existing-id&del_files=1` is requested
- **THEN** the system SHALL send DeleteDownload with DeleteFiles=true to the Manager

#### Scenario: History delete with archive parameter
- **WHEN** `?mode=history&name=delete&value=existing-id&archive=1` is requested
- **THEN** the system SHALL treat `archive` as a regular delete (parameter accepted but ignored)

#### Scenario: Delete non-existent history item
- **WHEN** `?mode=history&name=delete&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

### Requirement: Add file endpoint
The system SHALL respond to `POST /download/api?mode=addfile&cat=<category>` by accepting an NZB file as a multipart/form-data upload (field name `nzbfile`), parsing all metadata from the NZB XML, sending an `AddDownload` message to the DownloadManager, and returning the assigned download ID. It SHALL forward the `priority` parameter.

#### Scenario: Successful addfile via multipart
- **WHEN** a valid NZB is POSTed as multipart/form-data with field `nzbfile` and `?mode=addfile&cat=sonarr`
- **THEN** the system SHALL parse the NZB, extract VideoUrl, SubtitleUrl, Title, Channel, Duration, and Size from meta elements, send AddDownload to the DownloadManager, and respond with JSON `{"status":true,"nzo_ids":["<download-id>"]}`

#### Scenario: Addfile with priority
- **WHEN** a valid NZB is POSTed with `?mode=addfile&cat=sonarr&priority=-100`
- **THEN** the system SHALL send AddDownload with Priority=-100 to the DownloadManager

#### Scenario: Missing NZB file
- **WHEN** a POST is made with `?mode=addfile` but no `nzbfile` form field
- **THEN** the response SHALL be JSON `{"status":false,"error":"No NZB file uploaded"}` with HTTP 400

#### Scenario: Invalid NZB format
- **WHEN** the uploaded NZB file does not contain a parseable `X-FunkArr-Url` meta element
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid NZB format"}` with HTTP 400

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

### Requirement: Queue and history pagination
The system SHALL accept `start` (int, default 0) and `limit` (int, default 0) query parameters on queue and history endpoints. A limit of 0 means unlimited (return all items).

#### Scenario: Paginated queue
- **WHEN** `?mode=queue&start=5&limit=10` is requested
- **THEN** the response SHALL contain at most 10 queue slots starting from index 5
- **AND** `queue.noofslots_total` SHALL reflect the total count before pagination

#### Scenario: Paginated history
- **WHEN** `?mode=history&start=0&limit=25` is requested
- **THEN** the response SHALL contain at most 25 history slots starting from index 0

#### Scenario: Default pagination
- **WHEN** queue or history is requested without `start`/`limit`
- **THEN** the system SHALL return all items (start=0, limit=0 meaning unlimited)

#### Scenario: Limit zero means unlimited
- **WHEN** `?mode=queue&start=0&limit=0` is requested
- **THEN** the response SHALL return all queue items

#### Scenario: Queue category filter
- **WHEN** `?mode=queue&category=sonarr` is requested
- **THEN** the response SHALL contain only queue slots matching category "sonarr"

#### Scenario: History category filter
- **WHEN** `?mode=history&category=radarr` is requested
- **THEN** the response SHALL contain only history slots matching category "radarr"

### Requirement: Output query parameter
The system SHALL accept the `output` query parameter on all download API endpoints. The parameter value SHALL be accepted but ignored — the response format is always JSON.

#### Scenario: Output parameter accepted
- **WHEN** `?mode=version&output=json` is requested
- **THEN** the response SHALL be JSON `{"version":"4.3.3"}` (same as without the parameter)

#### Scenario: Output parameter absent
- **WHEN** `?mode=version` is requested without `output`
- **THEN** the response SHALL be JSON `{"version":"4.3.3"}`

### Requirement: Unknown mode
The system SHALL return HTTP 400 for unrecognized `mode` parameter values, including when `mode=queue` or `mode=history` receives an unrecognized `name` subcommand.

#### Scenario: Unknown mode
- **WHEN** `?mode=unknown` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid mode"}` with HTTP 400

#### Scenario: Unknown queue subcommand
- **WHEN** `?mode=queue&name=unknown` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid queue command"}` with HTTP 400

### Requirement: Download GET request parameters
The system SHALL bind the following query parameters on GET requests: `mode` (string), `name` (string), `value` (string), `start` (int), `limit` (int), `output` (string), `del_files` (int), `category` (string), `archive` (int).

#### Scenario: All parameters bound
- **WHEN** a GET request is made with `?mode=queue&start=0&limit=10&category=sonarr&del_files=1&archive=0`
- **THEN** all parameters SHALL be available in the request binding
