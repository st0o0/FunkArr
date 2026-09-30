## MODIFIED Requirements

### Requirement: Settings form
The UI SHALL display a settings form showing current configuration values. The settings view SHALL call versioned API endpoints at `/api/v1/config` and `/api/v1/setup/*`.

#### Scenario: Load current config
- **WHEN** the user navigates to settings
- **THEN** the UI SHALL load configuration from `GET /api/v1/config`

#### Scenario: Save settings
- **WHEN** the user modifies settings and clicks "Save"
- **THEN** the changes SHALL be persisted via `PUT /api/v1/config`

#### Scenario: Test connections
- **WHEN** the user clicks a "Test" button for Prowlarr
- **THEN** the UI SHALL call `POST /api/v1/setup/test-prowlarr`
