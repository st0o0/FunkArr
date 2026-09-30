## MODIFIED Requirements

### Requirement: First-run detection and wizard redirect
The system SHALL detect when FunkArr has not been configured (no API key set) and redirect users to the setup wizard automatically. The wizard SHALL call versioned API endpoints at `/api/v1/setup/*` and `/api/v1/config`.

#### Scenario: Wizard API calls use versioned routes
- **WHEN** the setup wizard tests a Prowlarr connection
- **THEN** it SHALL call `POST /api/v1/setup/test-prowlarr`

#### Scenario: Wizard saves config via versioned route
- **WHEN** the wizard completes and saves configuration
- **THEN** it SHALL call `PUT /api/v1/config`
