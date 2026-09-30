# SABnzbd Download API (Delta)

## MODIFIED Requirements

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a JSON object containing SABnzbd configuration including complete_dir, categories, sorting settings, and sorters.

#### Scenario: Config response structure
- **WHEN** `?mode=get_config` is requested
- **THEN** the response SHALL be JSON with `config.misc.complete_dir` set to the configured DownloadPath, and `config.categories` containing entries for "sonarr", "radarr", "tv", and "movies"

#### Scenario: Config category entries
- **WHEN** the config is returned
- **THEN** each entry in `config.categories` SHALL contain `name` (string), `order` (int), `dir` (empty string), `newzbin` (empty string), `priority` (0)

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
- **THEN** the response SHALL be JSON with a `status` object containing `paused` (bool, default false), `speedlimit` (string, default ""), `diskspace1` (string, free GB), `diskspace2` (string, free GB), `completedir` (string, configured download path), and `speed` (string, aggregate bytes/second of active downloads)

#### Scenario: Skip dashboard parameter accepted
- **WHEN** `?mode=fullstatus&skip_dashboard=1` is requested
- **THEN** the response SHALL be the same as without `skip_dashboard` (parameter accepted but ignored)

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` by querying the DownloadManager for current queue state and translating the response to SABnzbd JSON format. It SHALL accept optional `start` (int), `limit` (int), `category` (string), and `name` (string, subcommand) parameters.

#### Scenario: Queue with active downloads
- **WHEN** the DownloadManager has items in Queued or Processing status
- **THEN** each slot SHALL contain `nzo_id` (DownloadId string), `status` ("Queued" or "Downloading"), `filename` (title), `cat` (category), `mb` (total MB), `mbleft` (remaining MB), `percentage` (0-100), `timeleft` (formatted), `speed` (bytes/second string), `priority` ("Normal"), `index` (position)

#### Scenario: Queue pagination
- **WHEN** `?mode=queue&start=5&limit=10` is requested
- **THEN** the response SHALL contain at most 10 queue slots starting from index 5
- **AND** `queue.noofslots_total` SHALL reflect the total count before pagination

#### Scenario: Queue pagination with limit zero
- **WHEN** `?mode=queue&start=0&limit=0` is requested
- **THEN** the response SHALL return all queue items (limit=0 means unlimited)

#### Scenario: Queue category filter
- **WHEN** `?mode=queue&category=sonarr` is requested
- **THEN** the response SHALL contain only queue slots matching category "sonarr"

#### Scenario: Queue speed per slot
- **WHEN** a queue item has DownloadStatus Processing with progress data
- **THEN** `speed` SHALL be formatted as the download speed in bytes per second

### Requirement: Queue delete subcommand
The system SHALL respond to `GET /download/api?mode=queue&name=delete&value=<nzo_id>` by sending a `DeleteDownload` message to the DownloadManager. It SHALL accept an optional `del_files` parameter.

#### Scenario: Successful queue item deletion with del_files
- **WHEN** `?mode=queue&name=delete&value=existing-id&del_files=1` is requested
- **THEN** the system SHALL send DeleteDownload with DeleteFiles=true to the Manager, and respond with JSON `{"status":true}` on success

#### Scenario: Queue delete without del_files
- **WHEN** `?mode=queue&name=delete&value=existing-id` is requested without `del_files`
- **THEN** the system SHALL send DeleteDownload with DeleteFiles=false to the Manager

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` by querying the DownloadManager for history and translating the response to SABnzbd JSON format. It SHALL accept optional `start` (int), `limit` (int), `category` (string), and `name` (string, subcommand) parameters.

#### Scenario: History pagination
- **WHEN** `?mode=history&start=0&limit=25` is requested
- **THEN** the response SHALL contain at most 25 history slots starting from index 0

#### Scenario: History pagination with limit zero
- **WHEN** `?mode=history&start=0&limit=0` is requested
- **THEN** the response SHALL return all history items (limit=0 means unlimited)

#### Scenario: History category filter
- **WHEN** `?mode=history&category=radarr` is requested
- **THEN** the response SHALL contain only history slots matching category "radarr"

#### Scenario: History intermediate status values
- **WHEN** a history item has an intermediate download status
- **THEN** the `status` field SHALL be mapped as: Extracting → "Extracting", Moving → "Moving", Verifying → "Verifying"

### Requirement: Delete history item
The system SHALL respond to `GET /download/api?mode=history&name=delete&value=<nzo_id>` by sending a `DeleteDownload` message to the DownloadManager. It SHALL accept optional `del_files` and `archive` parameters.

#### Scenario: History delete with del_files
- **WHEN** `?mode=history&name=delete&value=existing-id&del_files=1` is requested
- **THEN** the system SHALL send DeleteDownload with DeleteFiles=true to the Manager

#### Scenario: History delete with archive parameter
- **WHEN** `?mode=history&name=delete&value=existing-id&archive=1` is requested
- **THEN** the system SHALL treat `archive` as a regular delete (parameter accepted but ignored)

### Requirement: Add file endpoint
The system SHALL respond to `POST /download/api?mode=addfile&cat=<category>` by accepting an NZB file as a multipart/form-data upload (field name `nzbfile`), parsing all metadata from the NZB XML, sending an `AddDownload` message to the DownloadManager, and returning the assigned download ID. It SHALL forward the `priority` parameter.

#### Scenario: Successful addfile with priority
- **WHEN** a valid NZB is POSTed with `?mode=addfile&cat=sonarr&priority=-100`
- **THEN** the system SHALL send AddDownload with Priority=-100 to the DownloadManager
- **AND** respond with JSON `{"status":true,"nzo_ids":["<download-id>"]}`

#### Scenario: Addfile without priority
- **WHEN** a valid NZB is POSTed with `?mode=addfile&cat=sonarr` without `priority`
- **THEN** the system SHALL send AddDownload with Priority=0 (default normal) to the DownloadManager

## ADDED Requirements

### Requirement: Download GET request parameters
The system SHALL bind the following query parameters on GET requests: `mode` (string), `name` (string), `value` (string), `start` (int), `limit` (int), `output` (string), `del_files` (int), `category` (string), `archive` (int).

#### Scenario: All parameters bound
- **WHEN** a GET request is made with `?mode=queue&start=0&limit=10&category=sonarr&del_files=1&archive=0`
- **THEN** all parameters SHALL be available in the request binding
