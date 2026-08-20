## Purpose

REST API for reading and updating FunkArr configuration, system status checks, and connection testing for external services (Prowlarr, Sonarr/Radarr, FFmpeg, Mediathek).

## Requirements

### Requirement: Get current config
The system SHALL expose `GET /api/v1/config` returning the current FunkArrOptions as JSON with sensitive fields (API keys for arr connections) masked.

#### Scenario: Return config with masked keys
- **WHEN** a client sends `GET /api/config?apikey=<valid>`
- **THEN** the system SHALL return the current config with arr instance API keys masked (e.g., `"●●●●●●ab12"`) but the FunkArr API key unmasked (the caller already knows it)

#### Scenario: Unauthenticated request
- **WHEN** a client sends `GET /api/v1/config` without a valid apikey
- **THEN** the system SHALL return 401

### Requirement: Update config
The system SHALL expose `PUT /api/v1/config` accepting a partial config update and persisting it to `data/config.json`.

#### Scenario: Update download settings
- **WHEN** a client sends `PUT /api/v1/config` with `{ "concurrentDownloads": 5 }`
- **THEN** the system SHALL update the config file and apply the change

#### Scenario: Update arr connections
- **WHEN** a client sends `PUT /api/v1/config` with new Prowlarr and ArrInstances entries
- **THEN** the system SHALL persist the connection details to `data/config.json`

### Requirement: System status endpoint
The system SHALL expose `GET /api/v1/setup/status` returning the overall system health: configured state, arr connection status, FFmpeg availability, path writability, Mediathek reachability, and ruleset count.

#### Scenario: Fully configured system
- **WHEN** all services are reachable and paths are writable
- **THEN** the response SHALL include `configured: true` with all checks passing

#### Scenario: Partially configured
- **WHEN** FFmpeg is not found but everything else works
- **THEN** the response SHALL include `configured: true` with `ffmpeg: { found: false }`

### Requirement: Test Prowlarr connection
The system SHALL expose `POST /api/v1/setup/test-prowlarr` accepting a URL and API key, and testing the connection by calling Prowlarr's `/api/v1/health` endpoint.

#### Scenario: Prowlarr reachable
- **WHEN** the provided URL and API key are valid
- **THEN** the response SHALL include `{ success: true }`

#### Scenario: Prowlarr unreachable
- **WHEN** the URL is unreachable
- **THEN** the response SHALL include `{ success: false, error: "<reason>" }`

### Requirement: Test arr instance connection
The system SHALL expose `POST /api/v1/setup/test-arr` accepting a URL, API key, and type (Sonarr/Radarr), and testing the connection by calling the instance's `/api/v3/system/status` endpoint.

#### Scenario: Sonarr reachable
- **WHEN** a valid Sonarr URL and API key are provided
- **THEN** the response SHALL include `{ success: true, version: "4.x.x" }`

#### Scenario: Radarr reachable
- **WHEN** a valid Radarr URL and API key are provided
- **THEN** the response SHALL include `{ success: true, version: "5.x.x" }`

### Requirement: Test paths
The system SHALL expose `POST /api/v1/setup/test-paths` accepting download and temp paths and verifying write access.

#### Scenario: Paths writable
- **WHEN** both paths exist and are writable
- **THEN** the response SHALL include `{ downloadPath: { ok: true }, tempPath: { ok: true } }`

#### Scenario: Path not writable
- **WHEN** the download path is not writable
- **THEN** the response SHALL include `{ downloadPath: { ok: false, error: "Permission denied" } }`

### Requirement: Test FFmpeg
The system SHALL expose `POST /api/v1/setup/test-ffmpeg` that runs `ffmpeg -version` and returns availability and version.

#### Scenario: FFmpeg found
- **WHEN** FFmpeg is installed and on the PATH
- **THEN** the response SHALL include `{ found: true, version: "7.1" }`

#### Scenario: FFmpeg not found
- **WHEN** FFmpeg is not installed
- **THEN** the response SHALL include `{ found: false }`

### Requirement: Test Mediathek API
The system SHALL expose `POST /api/v1/setup/test-mediathek` that pings the MediathekViewWeb API.

#### Scenario: Mediathek reachable
- **WHEN** the Mediathek API responds
- **THEN** the response SHALL include `{ reachable: true }`

#### Scenario: Mediathek unreachable
- **WHEN** the Mediathek API times out
- **THEN** the response SHALL include `{ reachable: false, error: "<reason>" }`

### Requirement: Config persistence to data/config.json
The system SHALL read runtime config from `data/config.json` at startup, layered on top of `appsettings.json`. Config writes SHALL go to `data/config.json` only, never mutating `appsettings.json`.

#### Scenario: Config file loaded at startup
- **WHEN** `data/config.json` exists at startup
- **THEN** its values SHALL override matching values from `appsettings.json`

#### Scenario: Config file created on first write
- **WHEN** `data/config.json` does not exist and the wizard saves config
- **THEN** the system SHALL create `data/config.json` with the wizard values

#### Scenario: Appsettings not modified
- **WHEN** config is saved via the API
- **THEN** `appsettings.json` SHALL NOT be modified
