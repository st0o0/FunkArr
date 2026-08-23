## Purpose

UI view for displaying FunkArr system status as a read-only dashboard. Configuration is done via environment variables — the settings view does not edit or save configuration.

## Requirements

### Requirement: Settings form
The UI SHALL display a read-only status dashboard showing current system state. No configuration editing, no save button.

#### Scenario: Show current status
- **WHEN** the user navigates to settings
- **THEN** the UI SHALL display current system status loaded from `GET /api/v1/setup/status`

#### Scenario: Display API key for copying
- **WHEN** the settings page is displayed
- **THEN** the API key SHALL be shown with a "Copy" button for pasting into Sonarr/Radarr/Prowlarr

#### Scenario: Connection status via validation
- **WHEN** the user wants to check Arr connectivity
- **THEN** they SHALL be directed to the Setup Guide or enter credentials temporarily for a one-off validation check

#### Scenario: No save functionality
- **WHEN** the settings page is displayed
- **THEN** there SHALL be no "Save" button and no editable configuration fields. A note SHALL explain that configuration is done via environment variables.

### Requirement: System info display
The settings view SHALL display FFmpeg version, path writability status, API key (with copy), Mediathek reachability, and a link to the Setup Guide.

#### Scenario: Show system info
- **WHEN** settings are displayed
- **THEN** the view SHALL show FFmpeg version (or "not found"), download and temp path writability status, Mediathek reachability, and the configured API key with a copy button

#### Scenario: Setup guide link
- **WHEN** settings are displayed
- **THEN** the view SHALL include a link to `/setup` labeled "Setup Guide"
