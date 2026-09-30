## ADDED Requirements

### Requirement: Version endpoint
The system SHALL respond to `GET /download/api?mode=version` with a JSON object containing a SABnzbd version string.

#### Scenario: Version response
- **WHEN** `?mode=version` is requested
- **THEN** the response SHALL be JSON `{"version":"4.3.3"}`

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a JSON object containing SABnzbd configuration including complete_dir and categories.

#### Scenario: Config response structure
- **WHEN** `?mode=get_config` is requested
- **THEN** the response SHALL be JSON with `config.misc.complete_dir` set to the configured DownloadPath, and `config.categories` containing entries for "sonarr", "radarr", "tv", and "movies"

#### Scenario: Config sorting disabled
- **WHEN** the config is returned
- **THEN** `config.misc.enable_tv_sorting` and `config.misc.enable_movie_sorting` SHALL be false

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a JSON object wrapping the current download queue.

#### Scenario: Empty queue
- **WHEN** no downloads are in progress
- **THEN** the response SHALL be JSON `{"queue":{"slots":[]}}`

#### Scenario: Queue item structure
- **WHEN** a download is in the queue
- **THEN** each slot SHALL contain `nzo_id` (string), `status` (string: "Queued", "Downloading", "Extracting"), `index` (int), `timeleft` (string), `mb` (string, total MB), `filename` (string), `cat` (string, category), `mbleft` (string, remaining MB), `percentage` (string, 0-100)

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` with a JSON object wrapping the download history.

#### Scenario: Empty history
- **WHEN** no downloads have completed
- **THEN** the response SHALL be JSON `{"history":{"slots":[]}}`

#### Scenario: History item structure
- **WHEN** a download is in history
- **THEN** each slot SHALL contain `nzo_id` (string), `name` (string), `nzb_name` (string), `category` (string), `bytes` (long), `download_time` (int, seconds), `storage` (string, file path), `status` (string: "Completed", "Failed")

### Requirement: Delete history item
The system SHALL respond to `GET /download/api?mode=history&name=delete&value=<nzo_id>` by removing the item from history.

#### Scenario: Successful deletion
- **WHEN** `?mode=history&name=delete&value=existing-id` is requested
- **THEN** the response SHALL be JSON `{"status":true}` and the item SHALL be removed from history

#### Scenario: Delete with file removal
- **WHEN** `?mode=history&name=delete&value=existing-id&del_files=1` is requested
- **THEN** the item SHALL be removed from history (file deletion is a no-op for stubbed implementation)

#### Scenario: Delete non-existent item
- **WHEN** `?mode=history&name=delete&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

### Requirement: Add file endpoint
The system SHALL respond to `POST /download/api?mode=addfile&cat=<category>` by accepting an NZB file in the request body, parsing the download URL and title from XML comments, and adding the item to the download queue.

#### Scenario: Successful addfile
- **WHEN** a valid NZB is POSTed with `?mode=addfile&cat=sonarr`
- **THEN** the response SHALL be JSON `{"status":true,"nzo_ids":["<generated-id>"]}` and the item SHALL appear in the queue

#### Scenario: Invalid NZB format
- **WHEN** the POST body does not contain parseable URL/title comments
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid NZB format"}` with HTTP 400

### Requirement: Unknown mode
The system SHALL return HTTP 400 for unrecognized `mode` parameter values.

#### Scenario: Unknown mode
- **WHEN** `?mode=unknown` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid mode"}` with HTTP 400
