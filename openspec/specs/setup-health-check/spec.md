## Purpose

Backend self-check endpoint for operational readiness verification. Checks API key configuration, MediathekViewWeb connectivity, directory permissions, own API endpoints, and FFmpeg availability.

## Requirements

### Requirement: Setup health check endpoint
The system SHALL expose `GET /api/health/setup` that returns a JSON object with the results of all operational readiness checks. The endpoint SHALL have no authentication. All checks SHALL run concurrently via `Task.WhenAll` to minimize response time.

#### Scenario: All checks pass
- **WHEN** `GET /api/health/setup` is requested and all prerequisites are met
- **THEN** the response status is 200 and every check entry has `"status": "ok"`

#### Scenario: Endpoint responds without authentication
- **WHEN** `GET /api/health/setup` is requested without an API key
- **THEN** the response status is 200 (not 401 or 403)

### Requirement: API key configuration check
The endpoint SHALL check whether `FunkArrOptions.ApiKey` differs from the default value `"funkarr-default-api-key"`. The response SHALL include a masked version of the key (last 3 characters visible, rest replaced with `*`) and the full key in cleartext for copy-to-clipboard.

#### Scenario: Custom API key configured
- **WHEN** `FunkArr__ApiKey` is set to `"my-secret-key"`
- **THEN** the `apiKey` check has `"status": "ok"`, `"masked": "************key"`, and `"value": "my-secret-key"`

#### Scenario: Default API key not changed
- **WHEN** `FunkArr__ApiKey` is not set (default value active)
- **THEN** the `apiKey` check has `"status": "warn"` and a message indicating the default key should be changed

### Requirement: MediathekViewWeb connectivity check
The endpoint SHALL perform an HTTP HEAD request to the MediathekViewWeb API base URL with a timeout of 3 seconds. The check SHALL report success if a 2xx or 3xx response is received.

#### Scenario: MediathekViewWeb reachable
- **WHEN** MediathekViewWeb responds with HTTP 200
- **THEN** the `mediathekViewWeb` check has `"status": "ok"`

#### Scenario: MediathekViewWeb unreachable
- **WHEN** MediathekViewWeb does not respond within 3 seconds
- **THEN** the `mediathekViewWeb` check has `"status": "fail"` and a message indicating the service is unreachable

### Requirement: Data directory check
The endpoint SHALL verify that the directory at `FunkArrOptions.DataPath` exists and is writable by attempting to create and delete a temporary file.

#### Scenario: Data directory writable
- **WHEN** the data directory exists and the process can write to it
- **THEN** the `dataDirectory` check has `"status": "ok"` and includes the resolved path

#### Scenario: Data directory not writable
- **WHEN** the data directory does not exist or is read-only
- **THEN** the `dataDirectory` check has `"status": "fail"` and a message with the path and the failure reason

### Requirement: Directory checks
The setup health check SHALL verify both the `complete` and `incomplete` subdirectories under `DownloadPath` instead of the single `DownloadPath` directory.

#### Scenario: Both directories exist and are writable
- **WHEN** `GET /api/health/setup` is requested and both `{DownloadPath}/complete` and `{DownloadPath}/incomplete` exist and are writable
- **THEN** the `downloadDirectory` check SHALL have `"status": "ok"` and report both paths

#### Scenario: Complete directory not writable
- **WHEN** `{DownloadPath}/complete` does not exist or is not writable
- **THEN** the `downloadDirectory` check SHALL have `"status": "fail"` with a message identifying the complete directory

#### Scenario: Incomplete directory not writable
- **WHEN** `{DownloadPath}/incomplete` does not exist or is not writable
- **THEN** the `downloadDirectory` check SHALL have `"status": "fail"` with a message identifying the incomplete directory

### Requirement: Indexer API self-test
The endpoint SHALL make an internal HTTP request to its own Newznab caps endpoint (`/index/api?t=caps&apikey=<configured-key>`) and verify it returns a valid XML response with status 200.

#### Scenario: Indexer API responds
- **WHEN** the internal caps request returns HTTP 200 with valid XML
- **THEN** the `indexerApi` check has `"status": "ok"`

#### Scenario: Indexer API fails
- **WHEN** the internal caps request fails or returns non-200
- **THEN** the `indexerApi` check has `"status": "fail"` and a message describing the failure

### Requirement: Download API self-test
The endpoint SHALL make an internal HTTP request to its own SABnzbd version endpoint (`/download/api?mode=version&apikey=<configured-key>`) and verify it returns a valid JSON response with status 200.

#### Scenario: Download API responds
- **WHEN** the internal version request returns HTTP 200 with valid JSON
- **THEN** the `downloadApi` check has `"status": "ok"`

#### Scenario: Download API fails
- **WHEN** the internal version request fails or returns non-200
- **THEN** the `downloadApi` check has `"status": "fail"` and a message describing the failure

### Requirement: FFmpeg availability check
The endpoint SHALL check whether `ffmpeg` is available on the system PATH by attempting to run `ffmpeg -version`. This check SHALL be non-critical -- a failure results in `"status": "warn"`, not `"fail"`.

#### Scenario: FFmpeg available
- **WHEN** `ffmpeg -version` executes successfully
- **THEN** the `ffmpeg` check has `"status": "ok"` and includes the detected version string

#### Scenario: FFmpeg not found
- **WHEN** `ffmpeg` is not on the PATH
- **THEN** the `ffmpeg` check has `"status": "warn"` and a message indicating FFmpeg is optional but needed for downloads

### Requirement: Health check response structure
The response SHALL be a JSON object with a top-level `checks` object containing named check results, and a `connectionInfo` object with the values needed by the setup guide. Each check SHALL have `status` (`"ok"`, `"warn"`, or `"fail"`), an optional `message`, and check-specific fields.

#### Scenario: Response includes connection info
- **WHEN** `GET /api/health/setup` is requested
- **THEN** the response includes `connectionInfo` with `indexerApiPath` (`"/index/api"`), `downloadApiPath` (`"/download/api"`), and `defaultPort` (`5000`)

#### Scenario: Response shape
- **WHEN** `GET /api/health/setup` is requested
- **THEN** the response matches the structure `{ "checks": { "<name>": { "status": "ok"|"warn"|"fail", "message?": "...", ... } }, "connectionInfo": { ... } }`
