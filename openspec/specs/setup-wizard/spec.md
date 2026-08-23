## Purpose

Multi-step setup guide for connecting FunkArr with Prowlarr/Sonarr/Radarr. The wizard does not configure FunkArr itself (configuration is done via environment variables) -- it guides the user through connecting external services and verifying system health.

## Requirements

### Requirement: Prowlarr mode selection
The wizard SHALL allow the user to indicate whether they use Prowlarr, toggling which integration instructions are displayed. This is a UI-only toggle, not a persisted configuration.

#### Scenario: With Prowlarr selected
- **WHEN** the user indicates they use Prowlarr
- **THEN** the wizard SHALL show Prowlarr indexer setup instructions and Sonarr/Radarr download-client-only instructions

#### Scenario: Without Prowlarr selected
- **WHEN** the user indicates they do not use Prowlarr
- **THEN** the wizard SHALL show Sonarr/Radarr instructions for both indexer and download client setup

### Requirement: Prowlarr connection step
The wizard SHALL display instructions for adding FunkArr as a Newznab indexer in Prowlarr, with copyable URL and API key fields. The user MAY optionally enter their Prowlarr URL and API key to verify the connection. Entered credentials SHALL remain in browser memory only and SHALL NOT be sent to any persistence endpoint.

#### Scenario: Display indexer instructions
- **WHEN** the Prowlarr step is active
- **THEN** the wizard SHALL display copyable fields for FunkArr's URL and API key with step-by-step instructions for adding a Newznab Custom indexer

#### Scenario: Optional connection test
- **WHEN** the user enters a Prowlarr URL and API key and clicks "Test"
- **THEN** the system SHALL call `POST /api/v1/setup/test-prowlarr` with the provided credentials and display the result

#### Scenario: Credentials not persisted
- **WHEN** the user enters Prowlarr credentials for testing
- **THEN** the credentials SHALL exist only in Vue component state and SHALL NOT be sent to any config save endpoint

### Requirement: Arr instance connections
The wizard SHALL display instructions for adding FunkArr as a download client (and optionally indexer) in Sonarr/Radarr. The user MAY optionally enter Arr instance URLs and API keys to verify connections. Entered credentials SHALL remain in browser memory only.

#### Scenario: Display download client instructions
- **WHEN** the Arr instance step is active
- **THEN** the wizard SHALL display copyable fields for FunkArr's URL and API key with step-by-step instructions for adding a SABnzbd download client

#### Scenario: Optional connection test
- **WHEN** the user enters an Arr instance URL and API key and clicks "Test"
- **THEN** the system SHALL call `POST /api/v1/setup/test-arr` with the provided credentials and display the result

#### Scenario: Multiple instances testable
- **WHEN** the user adds multiple Arr instances for testing
- **THEN** each SHALL be testable independently and results SHALL be displayed per instance

### Requirement: Self-check step
The wizard SHALL display a self-check showing FunkArr's own health: API key presence, FFmpeg availability, path writability, and Mediathek reachability. For any failing check, the wizard SHALL show fix guidance with the specific environment variable name to set.

#### Scenario: All self-checks pass
- **WHEN** all self-checks succeed
- **THEN** the wizard SHALL display green indicators for each check

#### Scenario: Self-check fails with guidance
- **WHEN** the download path is not writable
- **THEN** the wizard SHALL display a failure indicator with guidance like "Set `FunkArr__Download__DownloadPath` to a writable path in your docker-compose.yml"

#### Scenario: Self-check data source
- **WHEN** the self-check step loads
- **THEN** it SHALL call `GET /api/v1/setup/status` to retrieve current system health

### Requirement: Verification step
The wizard SHALL offer an optional verification step where the user can run `POST /api/v1/setup/validate` with temporarily entered Prowlarr and Arr credentials to confirm FunkArr is correctly registered in those systems.

#### Scenario: Full verification pass
- **WHEN** the user runs verification and FunkArr is registered in all entered Arr instances
- **THEN** the wizard SHALL display all green checkmarks

#### Scenario: Registration not found
- **WHEN** FunkArr is not registered as a download client in a tested Sonarr instance
- **THEN** the wizard SHALL display a warning with instructions for how to add it

### Requirement: Paths and downloads step
The wizard SHALL display the currently configured download path, temp path, and concurrent downloads as read-only information. For any path that is not writable, the wizard SHALL show fix guidance with the environment variable name.

#### Scenario: Display current paths
- **WHEN** the paths step is active
- **THEN** the wizard SHALL show the configured `DownloadPath`, `TempPath`, and `ConcurrentDownloads` as read-only values

#### Scenario: Path not writable
- **WHEN** the configured download path is not writable
- **THEN** the wizard SHALL show a warning with guidance to fix the Docker volume mount or set `FunkArr__Download__DownloadPath`
