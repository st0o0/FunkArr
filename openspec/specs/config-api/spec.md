## Purpose

REST API for system status checks and connection testing for external services (Prowlarr, Sonarr/Radarr, FFmpeg, Mediathek). Configuration is done via environment variables and appsettings.json — there is no runtime config read/write API.

## Requirements

### Requirement: System status endpoint
The system SHALL expose `GET /api/v1/setup/status` returning self-health checks: API key presence, FFmpeg availability, path writability, and Mediathek reachability. The endpoint SHALL NOT check Prowlarr or ArrInstance connectivity (no stored credentials).

#### Scenario: Fully configured system
- **WHEN** FFmpeg is available, paths are writable, Mediathek is reachable, and an API key is set
- **THEN** the response SHALL include `configured: true` with all self-checks passing

#### Scenario: Partially configured
- **WHEN** FFmpeg is not found but everything else works
- **THEN** the response SHALL include `configured: false` with `ffmpeg: { found: false }`

#### Scenario: No Prowlarr or ArrInstance checks
- **WHEN** the status endpoint is called
- **THEN** the response SHALL NOT include Prowlarr or ArrInstance connectivity results (those are only available via `POST /api/v1/setup/validate` with per-request credentials)

### Requirement: Test Prowlarr connection
The system SHALL expose `POST /api/v1/setup/test-prowlarr` accepting a URL and API key, and testing the connection by calling Prowlarr's `/api/v1/health` endpoint.

#### Scenario: Prowlarr reachable
- **WHEN** the provided URL and API key are valid
- **THEN** the response SHALL include `{ Success: true, StatusCode: 200, Error: null }`

#### Scenario: Prowlarr unreachable
- **WHEN** the URL is unreachable
- **THEN** the response SHALL include `{ Success: false, StatusCode: 0, Error: "<reason>" }`

### Requirement: Test arr instance connection
The system SHALL expose `POST /api/v1/setup/test-arr` accepting a URL, API key, and type (Sonarr/Radarr), and testing the connection by calling the instance's `/api/v3/system/status` endpoint.

#### Scenario: Sonarr reachable
- **WHEN** a valid Sonarr URL and API key are provided
- **THEN** the response SHALL include `{ Success: true, StatusCode: 200, Error: null }`

#### Scenario: Radarr reachable
- **WHEN** a valid Radarr URL and API key are provided
- **THEN** the response SHALL include `{ Success: true, StatusCode: 200, Error: null }`

### Requirement: Test paths
The system SHALL expose `POST /api/v1/setup/test-paths` accepting download and temp paths and verifying write access.

#### Scenario: Paths writable
- **WHEN** both paths exist and are writable
- **THEN** the response SHALL include `{ downloadOk: true, tempOk: true }`

#### Scenario: Path not writable
- **WHEN** the download path is not writable
- **THEN** the response SHALL include `{ downloadOk: false, tempOk: true }`

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

### Requirement: Validate configuration
The system SHALL expose `POST /api/v1/setup/validate` accepting Prowlarr and ArrInstance credentials per-request and returning validation results. Credentials SHALL NOT be persisted.

#### Scenario: Valid configuration with per-request credentials
- **WHEN** a client sends Prowlarr URL/key and ArrInstance URL/key in the request body
- **THEN** the system SHALL validate connectivity and registration using those credentials and return results

#### Scenario: Credentials not persisted
- **WHEN** validation completes
- **THEN** the supplied Prowlarr and ArrInstance credentials SHALL NOT be written to any configuration file or options class
