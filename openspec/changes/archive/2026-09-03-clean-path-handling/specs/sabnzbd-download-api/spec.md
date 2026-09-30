## MODIFIED Requirements

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` by querying the DownloadManager for history and translating the response to SABnzbd JSON format. The `storage` field SHALL be derived by resolving `RelativePath` against `DownloadOptions.CompletePath` and extracting the directory.

#### Scenario: Completed download storage path
- **WHEN** the history endpoint builds a `HistorySlot` for a completed download
- **AND** the internal `RelativePath` is `"tv/Show.S01E01/Show.S01E01.mkv"`
- **THEN** the `storage` field SHALL be `"{CompletePath}/tv/Show.S01E01"` (directory of the resolved absolute path)

#### Scenario: Failed download storage path
- **WHEN** the history endpoint builds a `HistorySlot` for a failed download
- **AND** `RelativePath` is null or empty
- **THEN** the `storage` field SHALL be null

#### Scenario: History with completed downloads
- **WHEN** the DownloadManager has items in history
- **THEN** each slot SHALL contain `nzo_id` (DownloadId string), `name` (title), `nzb_name` (title + ".nzb"), `category` (category), `bytes` (total bytes), `download_time` (seconds), `storage` (resolved directory path), `status` ("Completed", "Failed", "Extracting", "Moving", or "Verifying"), `fail_message` (error string or empty), `completed_on` (Unix timestamp)

#### Scenario: Empty history
- **WHEN** no downloads have completed or failed
- **THEN** the response SHALL be JSON `{"history":{"noofslots":0,"slots":[]}}`

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` by querying the DownloadManager for current queue state and translating the response to SABnzbd JSON format. The `WorkerStatusResult` no longer contains `FilePath`.

#### Scenario: Queue with active downloads
- **WHEN** the DownloadManager has items in Queued or Processing status
- **THEN** each slot SHALL contain `nzo_id` (DownloadId string), `status` ("Queued" or "Downloading"), `filename` (title), `cat` (category), `mb` (total MB), `mbleft` (remaining MB), `percentage` (0-100), `timeleft` (formatted), `speed` (bytes/second string), `priority` ("Normal"), `index` (position)
- **AND** the slot SHALL NOT contain a file path field
