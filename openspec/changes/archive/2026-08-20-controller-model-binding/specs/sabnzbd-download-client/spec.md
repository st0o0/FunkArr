## MODIFIED Requirements

### Requirement: Version endpoint
The system SHALL respond to `GET /download/api?mode=version` with a SABnzbd-compatible version response. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Version request
- **WHEN** a client sends `GET /download/api?mode=version&apikey=<key>`
- **THEN** the system returns a typed `SabnzbdVersionResponse` JSON object with version string (e.g., `{"version":"4.3.3"}`)

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a typed `SabnzbdConfigResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Config request
- **WHEN** a client sends `GET /download/api?mode=get_config&apikey=<key>`
- **THEN** the system returns a typed `SabnzbdConfigResponse` with `config.misc.complete_dir` and `config.categories` array

### Requirement: Add download
The system SHALL accept `POST /download/api?mode=addfile` with an NZB file upload via `[FromForm] IFormFile` model binding, extract the real download URL from the fake NZB's XML comments, and enqueue the download. The response SHALL use typed `SabnzbdAddFileResponse`.

#### Scenario: Sonarr sends a download request
- **WHEN** Sonarr sends `POST /download/api?mode=addfile` with a fake NZB file as multipart form data
- **THEN** the system binds the file via `[FromForm] IFormFile`, extracts the real URL from the NZB, creates a download job, and returns a typed response with `status: true` and the job's `nzo_ids`

#### Scenario: No file uploaded
- **WHEN** a client sends `mode=addfile` without a file attachment
- **THEN** the system returns a typed `SabnzbdAddFileResponse` with `status: false` and an error message

#### Scenario: Invalid NZB file
- **WHEN** a client sends `mode=addfile` with a file that contains no extractable download URL
- **THEN** the system returns a typed `SabnzbdAddFileResponse` with `status: false` and an error message

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a typed `SabnzbdQueueResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Active downloads in queue
- **WHEN** there are 2 active downloads and a client requests `mode=queue`
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` containing 2 entries, each with `nzo_id`, `filename`, `status`, `percentage`, `mb`, `mbleft`, and `timeleft`

#### Scenario: Empty queue
- **WHEN** there are no active downloads
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` as an empty array

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` with a typed `SabnzbdHistoryResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Completed downloads in history
- **WHEN** there are completed downloads and a client requests `mode=history`
- **THEN** the system returns a typed `SabnzbdHistoryResponse` with `history.slots` containing entries with `nzo_id`, `name`, `status`, `storage`, and `completed`

## ADDED Requirements

### Requirement: Typed SABnzbd response models
All SabnzbdController endpoints SHALL return typed response records from `FunkArr.Api.Models`. All SABnzbd-specific property names (snake_case) SHALL use `[JsonPropertyName]` attributes to maintain Sonarr/Radarr compatibility with the global camelCase naming policy.

#### Scenario: Snake_case JSON property names preserved
- **WHEN** Sonarr requests `mode=queue`
- **THEN** the response JSON SHALL use SABnzbd-compatible snake_case property names (`nzo_id`, `mbleft`, `timeleft`, `complete_dir`) via `[JsonPropertyName]` attributes

#### Scenario: OpenAPI schema completeness
- **WHEN** the OpenAPI spec is generated
- **THEN** all SABnzbd API response schemas SHALL be fully typed with correct property names
