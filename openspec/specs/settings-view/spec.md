## Purpose

UI view for displaying and editing FunkArr configuration: connections, paths, download settings, API key management, and system info.

## Requirements

### Requirement: Settings form
The UI SHALL display a settings form showing current configuration values: connections (Prowlarr, Sonarr/Radarr instances, Mediathek), paths, download settings, and system info.

#### Scenario: Show current config
- **WHEN** the user navigates to settings
- **THEN** the UI SHALL display current values for all configurable fields, loaded from the config API

#### Scenario: Connection status indicators
- **WHEN** settings are displayed
- **THEN** each connection (Prowlarr, Sonarr, Radarr, Mediathek) SHALL show a live status indicator (connected/unreachable)

#### Scenario: Load current config
- **WHEN** the user navigates to settings
- **THEN** the UI SHALL load configuration from `GET /api/v1/config`

#### Scenario: Save settings
- **WHEN** the user modifies settings and clicks "Save"
- **THEN** the changes SHALL be persisted via `PUT /api/v1/config`

#### Scenario: Test connections
- **WHEN** the user clicks a "Test" button for Prowlarr
- **THEN** the UI SHALL call `POST /api/v1/setup/test-prowlarr`

### Requirement: API key management
The settings view SHALL display the current API key (masked) with options to copy or regenerate it.

#### Scenario: Copy API key
- **WHEN** the user clicks "Copy"
- **THEN** the full API key SHALL be copied to the clipboard

#### Scenario: Regenerate API key
- **WHEN** the user clicks "Regenerate"
- **THEN** a new API key SHALL be generated, saved to config, and the localStorage key updated

### Requirement: Re-run wizard
The settings view SHALL provide a button to re-run the setup wizard.

#### Scenario: Re-run wizard
- **WHEN** the user clicks "Re-run Setup Wizard"
- **THEN** the wizard SHALL open with current values pre-filled

### Requirement: System info display
The settings view SHALL display FFmpeg version, ruleset count, and persistence path.

#### Scenario: Show system info
- **WHEN** settings are displayed
- **THEN** the view SHALL show FFmpeg version (or "not found"), number of loaded ruleset topics, and the database path
