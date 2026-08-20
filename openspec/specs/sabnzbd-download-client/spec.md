## Purpose

SABnzbd-compatible JSON API surface that Sonarr/Radarr use as a download client. Accepts fake NZBs containing encoded download URLs, manages a download queue, and reports progress/history.

## Requirements

### Requirement: Version endpoint
The system SHALL respond to `GET /download/api?mode=version` with a SABnzbd-compatible version string.

#### Scenario: Version request
- **WHEN** a client sends `GET /download/api?mode=version&apikey=<key>`
- **THEN** the system returns a plain-text SABnzbd version string (e.g., "4.3.3")

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a JSON object containing at minimum the download directory configuration.

#### Scenario: Config request
- **WHEN** a client sends `GET /download/api?mode=get_config&apikey=<key>`
- **THEN** the system returns JSON with `misc.complete_dir` set to the configured download output path

### Requirement: Add download
The system SHALL accept `POST /download/api?mode=addfile` with an NZB file upload, extract the real download URL from the fake NZB's XML comments, and enqueue the download.

#### Scenario: Sonarr sends a download request
- **WHEN** Sonarr sends `POST /download/api?mode=addfile` with a fake NZB file as multipart form data
- **THEN** the system extracts the real URL from the NZB, creates a download job, and returns JSON with `status: true` and the job's `nzo_ids`

#### Scenario: Invalid NZB file
- **WHEN** a client sends `mode=addfile` with a file that contains no extractable download URL
- **THEN** the system returns JSON with `status: false` and an error message

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a JSON object listing active downloads and their progress.

#### Scenario: Active downloads in queue
- **WHEN** there are 2 active downloads and a client requests `mode=queue`
- **THEN** the system returns JSON with `queue.slots` containing 2 entries, each with `nzo_id`, `filename`, `status` (Downloading/Queued), `percentage`, `mb`, `mbleft`, and `timeleft`

#### Scenario: Empty queue
- **WHEN** there are no active downloads
- **THEN** the system returns JSON with `queue.slots` as an empty array

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` with a JSON object listing completed and failed downloads.

#### Scenario: Completed downloads in history
- **WHEN** there are completed downloads and a client requests `mode=history`
- **THEN** the system returns JSON with `history.slots` containing entries with `nzo_id`, `name`, `status` (Completed/Failed), `storage` (output path), and `completed` (timestamp)

### Requirement: Download path mapping
The system SHALL support a configurable path mapping between the internal download path and the path as seen by Sonarr/Radarr (for Docker volume mount differences).

#### Scenario: Path mapping configured
- **WHEN** the path mapping is configured as `/app/downloads:/media/downloads` and a download completes to `/app/downloads/show.mkv`
- **THEN** the history endpoint reports `storage` as `/media/downloads/show.mkv`

### Requirement: API key validation
The system SHALL validate the `apikey` query parameter on all SABnzbd endpoints.

#### Scenario: Missing API key
- **WHEN** a client sends a request without `apikey`
- **THEN** the system returns JSON with `status: false` and an authentication error
