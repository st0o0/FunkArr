## ADDED Requirements

### Requirement: Arr service credential inputs
Each service configuration step (step 3+) SHALL display two input fields above the action buttons: a URL text input (placeholder: `http://sonarr:8989`) and an API Key password input. Both fields SHALL be required before the Create buttons become enabled.

#### Scenario: Empty inputs disable buttons
- **WHEN** the service step renders with empty URL and API Key fields
- **THEN** all Create buttons for that service are disabled

#### Scenario: Both fields filled enables buttons
- **WHEN** the user enters both a URL and an API Key
- **THEN** the Create buttons for that service become enabled

#### Scenario: URL placeholder per service
- **WHEN** the Prowlarr step renders
- **THEN** the URL placeholder is `http://prowlarr:9696`
- **WHEN** the Sonarr step renders
- **THEN** the URL placeholder is `http://sonarr:8989`
- **WHEN** the Radarr step renders
- **THEN** the URL placeholder is `http://radarr:7878`

### Requirement: Create Indexer button
Each service configuration step SHALL display a "Create Indexer" button that sends a POST to the corresponding `/api/setup/{service}/indexer` endpoint with the entered URL and API Key. The button SHALL show loading state during the request, a success state on success, and an error message on failure.

#### Scenario: Successful indexer creation
- **WHEN** the user clicks "Create Indexer" and the API returns `{ "success": true }`
- **THEN** the button shows a success state (checkmark + "Created")

#### Scenario: Failed indexer creation
- **WHEN** the user clicks "Create Indexer" and the API returns `{ "success": false, "error": "..." }`
- **THEN** the error message is displayed below the button

#### Scenario: Loading state
- **WHEN** the user clicks "Create Indexer" and the request is in flight
- **THEN** the button shows a spinner and is disabled

### Requirement: Create Download Client button
The Sonarr and Radarr configuration steps SHALL display a "Create Download Client" button alongside the "Create Indexer" button. It SHALL send a POST to `/api/setup/{service}/download-client`. The Prowlarr step SHALL NOT show this button.

#### Scenario: Sonarr shows both buttons
- **WHEN** the Sonarr configuration step renders with valid inputs
- **THEN** both "Create Indexer" and "Create Download Client" buttons are shown

#### Scenario: Prowlarr shows only indexer button
- **WHEN** the Prowlarr configuration step renders
- **THEN** only the "Create Indexer" button is shown

#### Scenario: Independent button state
- **WHEN** "Create Indexer" succeeds but "Create Download Client" has not been clicked
- **THEN** "Create Indexer" shows success and "Create Download Client" remains in idle state

### Requirement: Manual configuration fallback
Each service configuration step SHALL include a collapsed "Configure manually" section below the Create buttons. When expanded, it SHALL show the existing copy-paste configuration table with all fields and copy buttons. The section SHALL be collapsed by default.

#### Scenario: Collapsed by default
- **WHEN** the service step renders
- **THEN** the manual configuration section is collapsed

#### Scenario: Expand manual config
- **WHEN** the user clicks "Configure manually"
- **THEN** the copy-paste table expands showing all field values with copy buttons

#### Scenario: Manual fallback content unchanged
- **WHEN** the manual section is expanded on the Sonarr step
- **THEN** the table shows the same fields as before: Name, Host, Port, URL Base, API Key, Category

## MODIFIED Requirements

### Requirement: Prowlarr configuration step
When Prowlarr is selected, the guide SHALL show a configuration step with URL and API Key input fields and a "Create Indexer" button. Below the action area, a collapsed "Configure manually" section SHALL contain the existing field values for manual setup in Prowlarr.

The manual section SHALL display:
- **Name**: `FunkArr`
- **URL**: `http://<funkarr-host>:<port>` (placeholder)
- **API Path**: `/index/api`
- **API Key**: the configured API key with a copy-to-clipboard button
- **Categories**: `5000 (TV)`, `2000 (Movies)`

Each field SHALL have a copy-to-clipboard button for its value.

#### Scenario: Field values displayed
- **WHEN** the Prowlarr step is rendered and "Configure manually" is expanded
- **THEN** all fields are displayed with their values and copy buttons

#### Scenario: API key from health check
- **WHEN** the Prowlarr step is rendered
- **THEN** the API key shown in the manual section matches the `apiKey.value` from the health check response

#### Scenario: Copy to clipboard
- **WHEN** the user clicks the copy button next to API Path
- **THEN** `/index/api` is copied to the clipboard

### Requirement: Sonarr configuration step
When Sonarr is selected, the guide SHALL show a configuration step with URL and API Key input fields, a "Create Indexer" button, and a "Create Download Client" button. Below the action area, a collapsed "Configure manually" section SHALL contain the existing field values for manual setup in Sonarr.

The manual section SHALL display:
- **Name**: `FunkArr`
- **Host**: `<funkarr-host>` (placeholder)
- **Port**: `<funkarr-port>` (placeholder)
- **URL Base**: `/download/api`
- **API Key**: the configured API key with a copy-to-clipboard button
- **Category**: `tv`

Each field SHALL have a copy-to-clipboard button.

#### Scenario: Field values displayed
- **WHEN** the Sonarr step is rendered and "Configure manually" is expanded
- **THEN** all fields are displayed with their values and copy buttons

#### Scenario: URL Base value
- **WHEN** the Sonarr step is rendered and "Configure manually" is expanded
- **THEN** the URL Base field shows `/download/api`

### Requirement: Radarr configuration step
When Radarr is selected, the guide SHALL show a configuration step with URL and API Key input fields, a "Create Indexer" button, and a "Create Download Client" button. Below the action area, a collapsed "Configure manually" section SHALL contain the existing field values for manual setup in Radarr.

The manual section SHALL display:
- **Name**: `FunkArr`
- **Host**: `<funkarr-host>` (placeholder)
- **Port**: `<funkarr-port>` (placeholder)
- **URL Base**: `/download/api`
- **API Key**: the configured API key with a copy-to-clipboard button
- **Category**: `movies`

Each field SHALL have a copy-to-clipboard button.

#### Scenario: Field values displayed
- **WHEN** the Radarr step is rendered and "Configure manually" is expanded
- **THEN** all fields are displayed with their values and copy buttons

#### Scenario: Category differs from Sonarr
- **WHEN** the Radarr step is rendered and "Configure manually" is expanded
- **THEN** the Category field shows `movies` (not `tv`)
