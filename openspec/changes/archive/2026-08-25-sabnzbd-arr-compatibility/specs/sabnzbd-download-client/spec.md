## ADDED Requirements

### Requirement: Fullstatus endpoint
The system SHALL respond to `GET /download/api?mode=fullstatus` with a JSON object containing `completedir`. The `completedir` value SHALL be the configured `DownloadOptions.Path` with `PathMappingHelper` applied.

#### Scenario: Sonarr connection test reads completedir
- **WHEN** a client sends `GET /download/api?mode=fullstatus&apikey=<key>`
- **THEN** the system SHALL return `{ "completedir": "<mapped-path>" }`

#### Scenario: Completedir with path mapping
- **WHEN** `DownloadOptions.Path` is `/app/downloads` and `PathMapping` is `/app/downloads:/media/downloads`
- **THEN** `completedir` SHALL be `/media/downloads`

#### Scenario: Completedir without path mapping
- **WHEN** `DownloadOptions.Path` is `/media/downloads` and `PathMapping` is null
- **THEN** `completedir` SHALL be `/media/downloads`

### Requirement: Queue delete
The system SHALL respond to `GET /download/api?mode=queue&name=delete&value={nzoId}` by cancelling the specified download via `QueueActor.Cancel`. The response SHALL be `{ "status": true }`.

#### Scenario: Delete active download from queue
- **WHEN** a client sends `mode=queue&name=delete&value=abc123` and `abc123` is an active download
- **THEN** the system SHALL tell `QueueActor.Cancel("abc123")` and return `{ "status": true }`

#### Scenario: Delete queued download from queue
- **WHEN** a client sends `mode=queue&name=delete&value=abc123` and `abc123` is pending in queue
- **THEN** the system SHALL tell `QueueActor.Cancel("abc123")` and return `{ "status": true }`

#### Scenario: Delete non-existent nzoId from queue
- **WHEN** a client sends `mode=queue&name=delete&value=unknown`
- **THEN** the system SHALL tell `QueueActor.Cancel("unknown")` (which logs and ignores) and return `{ "status": true }`

### Requirement: History delete
The system SHALL respond to `GET /download/api?mode=history&name=delete&value={nzoId}` by removing the nzoId from history via `QueueActor.RemoveFromHistory`. The `DownloadRequestActor` shard entity SHALL NOT be deleted. The response SHALL be `{ "status": true }`.

#### Scenario: Delete completed download from history
- **WHEN** a client sends `mode=history&name=delete&value=abc123` and `abc123` is in completed history
- **THEN** the system SHALL tell `QueueActor.RemoveFromHistory("abc123")` and return `{ "status": true }`

#### Scenario: Delete non-existent nzoId from history
- **WHEN** a client sends `mode=history&name=delete&value=unknown`
- **THEN** the system SHALL tell `QueueActor.RemoveFromHistory("unknown")` (which logs and ignores) and return `{ "status": true }`

### Requirement: Queue pagination
The system SHALL accept optional `start` and `limit` query parameters on `mode=queue`. When provided, the queue slots SHALL be sliced using `Skip(start).Take(limit)`. When omitted, all slots SHALL be returned. The total slot count before pagination SHALL be available in the response.

#### Scenario: Paginated queue request
- **WHEN** a client sends `mode=queue&start=0&limit=10` and there are 25 queued items
- **THEN** the system SHALL return the first 10 slots

#### Scenario: Queue request without pagination
- **WHEN** a client sends `mode=queue` without `start` or `limit`
- **THEN** the system SHALL return all slots

### Requirement: History pagination
The system SHALL accept optional `start` and `limit` query parameters on `mode=history`. When provided, the history slots SHALL be sliced using `Skip(start).Take(limit)`. When omitted, all slots SHALL be returned.

#### Scenario: Paginated history request
- **WHEN** a client sends `mode=history&start=5&limit=10` and there are 30 history entries
- **THEN** the system SHALL return entries 5 through 14

#### Scenario: History request without pagination
- **WHEN** a client sends `mode=history` without `start` or `limit`
- **THEN** the system SHALL return all history slots

### Requirement: Retry failed download
The system SHALL respond to `GET /download/api?mode=retry&value={nzoId}` by querying the `DownloadRequestActor` for retry info (download URL, title, subtitle URL, category), then re-enqueueing via `QueueActor.Enqueue`. The response SHALL include the new nzoId.

#### Scenario: Retry a failed download
- **WHEN** a client sends `mode=retry&value=abc123` and `abc123` is a failed download with URL `https://example.com/video.mp4`
- **THEN** the system SHALL ask `DownloadRequestActor` for retry info, enqueue via `QueueActor.Enqueue`, and return `{ "status": true, "nzo_ids": ["<new-nzo-id>"] }`

#### Scenario: Retry a non-failed download
- **WHEN** a client sends `mode=retry&value=abc123` and `abc123` is not in "Failed" status
- **THEN** the system SHALL return `{ "status": false, "error": "Job is not in failed state" }`

### Requirement: Controller query parameter expansion
The `HandleGet` method SHALL accept optional `[FromQuery]` parameters: `name` (string?), `value` (string?), `start` (int?), `limit` (int?), `del_files` (int?). These SHALL be used for queue/history sub-operations and pagination.

#### Scenario: Compound query routing
- **WHEN** a client sends `mode=queue&name=delete&value=abc123`
- **THEN** the controller SHALL route to queue delete handling based on `name=delete`

### Requirement: Addfile accepts priority parameter
The `HandlePost` method SHALL accept an optional `priority` query parameter. The value SHALL be ignored (not used for scheduling).

#### Scenario: Radarr sends priority
- **WHEN** Radarr sends `POST /download/api?mode=addfile&cat=movies&priority=-100`
- **THEN** the system SHALL process the download normally, ignoring the priority value

## MODIFIED Requirements

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a typed `SabnzbdQueueResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Active downloads in queue include category
- **WHEN** there are active downloads with categories and a client requests `mode=queue`
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` containing entries that include `cat` field alongside `nzo_id`, `filename`, `status`, `percentage`, `mb`, `mbleft`, `timeleft`, `index`, `priority`, and `storage`

#### Scenario: Empty queue
- **WHEN** there are no active downloads
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` as an empty array

#### Scenario: Queue slot index and priority fields
- **WHEN** there are 3 items in the queue and a client requests `mode=queue`
- **THEN** each slot SHALL include `index` (0-based position in the array) and `priority` (`"Normal"`)

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` with a typed `SabnzbdHistoryResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Completed downloads include category
- **WHEN** there are completed downloads with categories and a client requests `mode=history`
- **THEN** the system returns a typed `SabnzbdHistoryResponse` with `history.slots` containing entries that include `cat` field alongside `nzo_id`, `name`, `status`, `storage`, `completed`, `fail_message`, `bytes`, `nzb_name`, and `download_time`

#### Scenario: History slot additional fields
- **WHEN** a completed download named "Show.S01E01.mkv" is in history
- **THEN** the slot SHALL include `bytes: 0`, `nzb_name` mirroring `name`, and `download_time: 0`

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a typed `SabnzbdConfigResponse` JSON object. The `mode` parameter SHALL be bound via `[FromQuery]`.

#### Scenario: Config request with configured categories
- **WHEN** a client sends `GET /download/api?mode=get_config&apikey=<key>` and `DownloadOptions.Category` contains `{"tv": "tv", "movies": "/data/movies"}`
- **THEN** the system returns a `SabnzbdConfigResponse` with `config.misc.complete_dir` set to `DownloadOptions.Path` and `config.categories` containing entries for `"tv"` and `"movies"` with their resolved paths

#### Scenario: Config includes pre_check_label and sorting
- **WHEN** a client sends `mode=get_config`
- **THEN** `config.misc` SHALL include `pre_check_label: false` and `config` SHALL include `sorters: []`

#### Scenario: Config request with no configured categories
- **WHEN** `DownloadOptions.Category` is empty
- **THEN** `config.categories` SHALL be an empty array

### Requirement: Typed SABnzbd response models
All SabnzbdController endpoints SHALL return typed response records from `FunkArr.Api.Contracts.Sabnzbd`. All SABnzbd-specific property names (snake_case) SHALL use `[JsonPropertyName]` attributes to maintain Sonarr/Radarr compatibility with the global camelCase naming policy.

#### Scenario: Snake_case JSON property names preserved
- **WHEN** Sonarr requests `mode=queue`
- **THEN** the response JSON SHALL use SABnzbd-compatible snake_case property names (`nzo_id`, `mbleft`, `timeleft`, `complete_dir`, `index`, `priority`) via `[JsonPropertyName]` attributes

#### Scenario: History response snake_case properties
- **WHEN** Sonarr requests `mode=history`
- **THEN** the response JSON SHALL include `nzb_name`, `download_time`, and `fail_message` with snake_case `[JsonPropertyName]` attributes

#### Scenario: OpenAPI schema completeness
- **WHEN** the OpenAPI spec is generated
- **THEN** all SABnzbd API response schemas SHALL be fully typed with correct property names including the new fields
