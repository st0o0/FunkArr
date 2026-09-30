## MODIFIED Requirements

### Requirement: Add download
The system SHALL accept `POST /download/api?mode=addfile` with an NZB file upload via `[FromForm] IFormFile` model binding and an optional `cat` query parameter, extract the real download URL from the fake NZB's XML comments, and enqueue the download with the category. The response SHALL use typed `SabnzbdAddFileResponse`.

#### Scenario: Sonarr sends a download request with category
- **WHEN** Sonarr sends `POST /download/api?mode=addfile&cat=tv` with a fake NZB file as multipart form data
- **THEN** the system SHALL extract the URL from the NZB, enqueue the download with category `"tv"`, and return a typed response with `status: true` and the job's `nzo_ids`

#### Scenario: Download request without category
- **WHEN** a client sends `POST /download/api?mode=addfile` without a `cat` parameter
- **THEN** the system SHALL enqueue the download with a null category

#### Scenario: No file uploaded
- **WHEN** a client sends `mode=addfile` without a file attachment
- **THEN** the system returns a typed `SabnzbdAddFileResponse` with `status: false` and an error message

#### Scenario: Invalid NZB file
- **WHEN** a client sends `mode=addfile` with a file that contains no extractable download URL
- **THEN** the system returns a typed `SabnzbdAddFileResponse` with `status: false` and an error message

### Requirement: SABnzbd addfile enqueues download
The SABnzbd controller SHALL route `mode=addfile` through `QueueCoordinator.Enqueue` instead of directly telling `DownloadQueueActor.EnqueueDownload`. The controller SHALL pass the category from the `cat` query parameter and receive the nzoId from QueueCoordinator's reply.

#### Scenario: Addfile routed through QueueCoordinator with category
- **WHEN** the SABnzbd controller receives `mode=addfile` with a download URL, title, and `cat=movies`
- **THEN** it SHALL ask `QueueCoordinator` with `Enqueue(url, title, subtitleUrl, category: "movies")` and receive the generated nzoId

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a typed `SabnzbdConfigResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Config request with configured categories
- **WHEN** a client sends `GET /download/api?mode=get_config&apikey=<key>` and `DownloadOptions.Category` contains `{"tv": "tv", "movies": "/data/movies"}`
- **THEN** the system returns a `SabnzbdConfigResponse` with `config.misc.complete_dir` set to `DownloadOptions.Path` and `config.categories` containing entries for `"tv"` and `"movies"` with their resolved paths

#### Scenario: Config request with no configured categories
- **WHEN** `DownloadOptions.Category` is empty
- **THEN** `config.categories` SHALL be an empty array

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a typed `SabnzbdQueueResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Active downloads in queue include category
- **WHEN** there are active downloads with categories and a client requests `mode=queue`
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` containing entries that include `cat` field alongside `nzo_id`, `filename`, `status`, `percentage`, `mb`, `mbleft`, and `timeleft`

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` with a typed `SabnzbdHistoryResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Completed downloads include category
- **WHEN** there are completed downloads with categories and a client requests `mode=history`
- **THEN** the system returns a typed `SabnzbdHistoryResponse` with `history.slots` containing entries that include `cat` field alongside `nzo_id`, `name`, `status`, `storage`, and `completed`
