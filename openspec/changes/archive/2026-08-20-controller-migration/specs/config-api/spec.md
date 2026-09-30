## MODIFIED Requirements

### Requirement: Get current config
The system SHALL expose `GET /api/v1/config` returning the current FunkArrOptions as JSON with sensitive fields (API keys for arr connections) masked.

#### Scenario: Return config with masked keys
- **WHEN** a client sends `GET /api/v1/config?apikey=<valid>`
- **THEN** the system SHALL return the current config with arr instance API keys masked but the FunkArr API key unmasked

#### Scenario: Unauthenticated request
- **WHEN** a client sends `GET /api/v1/config` without a valid apikey
- **THEN** the `ApiKeyMiddleware` SHALL return 401

### Requirement: Update config
The system SHALL expose `PUT /api/v1/config` accepting a partial config update and persisting it to `data/config.json`.

#### Scenario: Update download settings
- **WHEN** a client sends `PUT /api/v1/config` with `{ "concurrentDownloads": 5 }`
- **THEN** the system SHALL update the config file and apply the change

### Requirement: System status endpoint
The system SHALL expose `GET /api/v1/setup/status` returning the overall system health.

#### Scenario: Fully configured system
- **WHEN** all services are reachable and paths are writable
- **THEN** the response SHALL include `configured: true` with all checks passing

### Requirement: Test Prowlarr connection
The system SHALL expose `POST /api/v1/setup/test-prowlarr` accepting a URL and API key, and testing the connection.

#### Scenario: Prowlarr reachable
- **WHEN** the provided URL and API key are valid
- **THEN** the response SHALL include `{ success: true }`

### Requirement: Test arr instance connection
The system SHALL expose `POST /api/v1/setup/test-arr` accepting a URL, API key, and type.

#### Scenario: Sonarr reachable
- **WHEN** a valid Sonarr URL and API key are provided
- **THEN** the response SHALL include `{ success: true, version: "4.x.x" }`

### Requirement: Test paths
The system SHALL expose `POST /api/v1/setup/test-paths` accepting download and temp paths and verifying write access.

#### Scenario: Paths writable
- **WHEN** both paths exist and are writable
- **THEN** the response SHALL include path status

### Requirement: Test FFmpeg
The system SHALL expose `POST /api/v1/setup/test-ffmpeg` that runs `ffmpeg -version` and returns availability and version.

#### Scenario: FFmpeg found
- **WHEN** FFmpeg is installed and on the PATH
- **THEN** the response SHALL include `{ found: true, version: "7.1" }`

### Requirement: Test Mediathek API
The system SHALL expose `POST /api/v1/setup/test-mediathek` that pings the MediathekViewWeb API.

#### Scenario: Mediathek reachable
- **WHEN** the Mediathek API responds
- **THEN** the response SHALL include `{ reachable: true }`

## ADDED Requirements

### Requirement: Controller-based implementation
The setup and config endpoints SHALL be implemented as a single MVC controller (`SetupController`) in the `FunkArr.Api` namespace, combining the `/api/v1/setup` and `/api/v1/config` route groups.

#### Scenario: Versioned routes
- **WHEN** a client sends `GET /api/v1/setup/status?apikey=key`
- **THEN** the system SHALL route to `SetupController`

### Requirement: Typed response models
Setup and config responses SHALL use typed DTOs instead of anonymous objects.

#### Scenario: OpenAPI schema for status
- **WHEN** the OpenAPI spec is generated
- **THEN** the status response schema SHALL include all properties with their types (configured, ffmpeg, paths, mediathek, prowlarr, arrInstances)
