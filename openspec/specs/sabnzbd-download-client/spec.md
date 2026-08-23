## Purpose

SABnzbd-compatible JSON API surface that Sonarr/Radarr use as a download client. Accepts fake NZBs containing encoded download URLs, manages a download queue, and reports progress/history.

## Requirements

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

### Requirement: SABnzbd addfile enqueues download
The SABnzbd controller SHALL route `mode=addfile` through `QueueCoordinator.Enqueue` instead of directly telling `DownloadQueueActor.EnqueueDownload`. The controller SHALL receive the nzoId from QueueCoordinator's reply.

#### Scenario: Addfile routed through QueueCoordinator
- **WHEN** the SABnzbd controller receives `mode=addfile` with a download URL and title
- **THEN** it SHALL ask `QueueCoordinator` with `Enqueue(url, title, subtitleUrl)` and receive the generated nzoId

### Requirement: SABnzbd delete routes through QueueCoordinator
The SABnzbd controller SHALL route delete operations through `QueueCoordinator.Cancel` instead of directly modifying download state.

#### Scenario: Delete routed through QueueCoordinator
- **WHEN** the SABnzbd controller receives a delete request for nzoId "abc123"
- **THEN** it SHALL tell `QueueCoordinator` with `Cancel("abc123")`

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a typed `SabnzbdQueueResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Active downloads in queue
- **WHEN** there are 2 active downloads and a client requests `mode=queue`
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` containing 2 entries, each with `nzo_id`, `filename`, `status`, `percentage`, `mb`, `mbleft`, and `timeleft`

#### Scenario: Empty queue
- **WHEN** there are no active downloads
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` as an empty array

### Requirement: SABnzbd queue reads from DownloadRequestTracker
The SABnzbd controller SHALL query `QueueCoordinator.GetQueueOrder` for the ordered nzoId list, then fan out `GetStatus` to individual `DownloadRequestTracker` shard entities to build the queue response.

#### Scenario: Queue response built from tracker entities
- **WHEN** `mode=queue` is requested
- **THEN** the controller SHALL ask QueueCoordinator for ordered nzoIds, then ask each tracker entity for status, and assemble the SABnzbd queue JSON from the responses

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` with a typed `SabnzbdHistoryResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Completed downloads in history
- **WHEN** there are completed downloads and a client requests `mode=history`
- **THEN** the system returns a typed `SabnzbdHistoryResponse` with `history.slots` containing entries with `nzo_id`, `name`, `status`, `storage`, and `completed`

### Requirement: SABnzbd history reads from DownloadRequestTracker
The SABnzbd controller SHALL query completed/failed tracker entities for history entries instead of asking DownloadQueueActor.

#### Scenario: History response built from tracker entities
- **WHEN** `mode=history` is requested
- **THEN** the controller SHALL ask QueueCoordinator for completed job IDs, then ask each tracker entity for history entry data

### Requirement: Download path mapping
The system SHALL support a configurable path mapping between the internal download path and the path as seen by Sonarr/Radarr (for Docker volume mount differences).

#### Scenario: Path mapping configured
- **WHEN** the path mapping is configured as `/app/downloads:/media/downloads` and a download completes to `/app/downloads/show.mkv`
- **THEN** the history endpoint reports `storage` as `/media/downloads/show.mkv`

### Requirement: API key validation
The system SHALL validate the `apikey` query parameter on all SABnzbd endpoints. Authentication SHALL be handled by the centralized `ApiKeyMiddleware` instead of the inline `ValidateApiKey` method.

#### Scenario: Missing API key
- **WHEN** a client sends a request without `apikey`
- **THEN** the `ApiKeyMiddleware` SHALL return JSON with HTTP 401 and an authentication error

### Requirement: Controller-based implementation
The SABnzbd endpoints SHALL be implemented as an MVC controller (`SabnzbdController`) in the `FunkArr.Api` namespace, located in `src/FunkArr/Api/`. The controller SHALL be marked as version-neutral (no URL version segment).

#### Scenario: SABnzbd route unchanged
- **WHEN** a client sends `GET /download/api?mode=version&apikey=key`
- **THEN** the system SHALL route to `SabnzbdController` at the same `/download/api` path as before

### Requirement: OpenAPI tagging
The SABnzbd controller SHALL be tagged with `"SABnzbd Emulation"` for API documentation grouping.

#### Scenario: Scalar documentation grouping
- **WHEN** the OpenAPI spec is rendered in Scalar
- **THEN** SABnzbd endpoints SHALL appear under the "SABnzbd Emulation" group

### Requirement: Typed SABnzbd response models
All SabnzbdController endpoints SHALL return typed response records from `FunkArr.Api.Models`. All SABnzbd-specific property names (snake_case) SHALL use `[JsonPropertyName]` attributes to maintain Sonarr/Radarr compatibility with the global camelCase naming policy.

#### Scenario: Snake_case JSON property names preserved
- **WHEN** Sonarr requests `mode=queue`
- **THEN** the response JSON SHALL use SABnzbd-compatible snake_case property names (`nzo_id`, `mbleft`, `timeleft`, `complete_dir`) via `[JsonPropertyName]` attributes

#### Scenario: OpenAPI schema completeness
- **WHEN** the OpenAPI spec is generated
- **THEN** all SABnzbd API response schemas SHALL be fully typed with correct property names
