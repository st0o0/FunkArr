## Purpose

Multi-step setup wizard for first-run configuration: API key generation, Prowlarr/arr connections, paths, and verification checks.

## Requirements

### Requirement: First-run detection and wizard redirect
The system SHALL detect when FunkArr has not been configured (no API key set) and redirect users to the setup wizard automatically. The wizard SHALL call versioned API endpoints at `/api/v1/setup/*` and `/api/v1/config`.

#### Scenario: Unconfigured system
- **WHEN** a user opens FunkArr for the first time and no API key is configured
- **THEN** the UI SHALL display the setup wizard instead of the dashboard

#### Scenario: Already configured
- **WHEN** a user opens FunkArr and an API key is configured
- **THEN** the UI SHALL display the normal dashboard (queue view)

#### Scenario: Wizard API calls use versioned routes
- **WHEN** the setup wizard tests a Prowlarr connection
- **THEN** it SHALL call `POST /api/v1/setup/test-prowlarr`

#### Scenario: Wizard saves config via versioned route
- **WHEN** the wizard completes and saves configuration
- **THEN** it SHALL call `PUT /api/v1/config`

### Requirement: API key generation step
The wizard SHALL provide a step for generating or manually entering an API key.

#### Scenario: Generate API key
- **WHEN** the user clicks "Generate" in the API key step
- **THEN** the system SHALL generate a random API key and display it with a copy button

#### Scenario: Manual API key entry
- **WHEN** the user types a custom API key
- **THEN** the wizard SHALL accept it and proceed

### Requirement: Prowlarr mode selection
The wizard SHALL ask the user whether they use Prowlarr, branching the setup flow accordingly.

#### Scenario: With Prowlarr selected
- **WHEN** the user selects "With Prowlarr"
- **THEN** the wizard SHALL show the Prowlarr connection step followed by Sonarr/Radarr as download client only

#### Scenario: Without Prowlarr selected
- **WHEN** the user selects "Without Prowlarr"
- **THEN** the wizard SHALL show Sonarr/Radarr setup with instructions for both indexer and download client configuration

### Requirement: Prowlarr connection step
The wizard SHALL allow the user to enter a Prowlarr URL and API key, test the connection, and display instructions for adding FunkArr as a Newznab indexer.

#### Scenario: Prowlarr connection test succeeds
- **WHEN** the user enters a valid Prowlarr URL and API key and clicks "Test"
- **THEN** the system SHALL display a success indicator

#### Scenario: Prowlarr connection test fails
- **WHEN** the user enters an unreachable Prowlarr URL and clicks "Test"
- **THEN** the system SHALL display an error message with the failure reason

#### Scenario: Display indexer instructions
- **WHEN** the Prowlarr step is active
- **THEN** the wizard SHALL display copyable fields for URL (`http://funkarr:5000/api`) and API key with instructions for adding a Newznab Custom indexer

### Requirement: Arr instance connections
The wizard SHALL allow adding one or more Sonarr/Radarr instances with URL, API key, type selection, and connection testing.

#### Scenario: Add Sonarr instance
- **WHEN** the user clicks "Add" and selects Sonarr
- **THEN** a new connection form SHALL appear with URL, API key, and test button

#### Scenario: Multiple arr instances
- **WHEN** the user adds a Sonarr and a Radarr instance
- **THEN** both SHALL be stored in the config and both SHALL be testable independently

#### Scenario: Remove arr instance
- **WHEN** the user clicks the remove button on an arr instance
- **THEN** that instance SHALL be removed from the list

#### Scenario: Download client instructions (with Prowlarr)
- **WHEN** Prowlarr mode is active
- **THEN** the wizard SHALL show instructions for adding FunkArr as a SABnzbd download client only

#### Scenario: Full instructions (without Prowlarr)
- **WHEN** direct mode is active
- **THEN** the wizard SHALL show instructions for adding FunkArr as both a Newznab indexer AND a SABnzbd download client

### Requirement: Paths and downloads step
The wizard SHALL allow configuring the download path, temp path, concurrent downloads, and optional path mapping.

#### Scenario: Configure paths
- **WHEN** the user enters download and temp paths
- **THEN** the wizard SHALL validate the paths via the backend test endpoint

#### Scenario: Path mapping
- **WHEN** the user enters a path mapping (container:host format)
- **THEN** the mapping SHALL be stored in config for SABnzbd history path translation

### Requirement: Verification step
The wizard SHALL display a summary of all checks: FFmpeg availability, directory writability, arr connections, Mediathek API reachability, and ruleset count.

#### Scenario: All checks pass
- **WHEN** all verification checks succeed
- **THEN** the wizard SHALL display all green checkmarks and enable the "Finish Setup" button

#### Scenario: Some checks fail
- **WHEN** FFmpeg is not found
- **THEN** the wizard SHALL display a warning for FFmpeg but still allow completing setup (FFmpeg is needed for muxing, not basic operation)

### Requirement: Save wizard config
The wizard SHALL persist all configuration to `data/config.json` via the config API when the user clicks "Finish Setup".

#### Scenario: Config saved
- **WHEN** the user clicks "Finish Setup"
- **THEN** all wizard settings SHALL be written to `data/config.json` and the user SHALL be redirected to the dashboard
