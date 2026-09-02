## Purpose

Interactive stepper UI for setup verification and *arr configuration walkthrough. Guides users through health checks, service selection, and per-service configuration with copy-to-clipboard values.

## Requirements

### Requirement: Dashboard health widget
The Home view SHALL display a health status widget that calls `GET /api/health/setup` on mount and shows the result of each check as a status indicator. The widget SHALL auto-refresh every 30 seconds. Each check SHALL be displayed with a colored indicator: green for `"ok"`, yellow for `"warn"`, red for `"fail"`.

#### Scenario: All checks healthy
- **WHEN** the dashboard loads and all checks return `"ok"`
- **THEN** all indicators are green

#### Scenario: Warning check displayed
- **WHEN** the FFmpeg check returns `"warn"`
- **THEN** the FFmpeg indicator is yellow and the warning message is shown

#### Scenario: Failed check with fix hint
- **WHEN** the data directory check returns `"fail"` with a message
- **THEN** the indicator is red and the failure message is displayed inline

#### Scenario: Widget links to setup guide
- **WHEN** the health widget is displayed
- **THEN** a link or button navigates to `/setup`

### Requirement: Setup guide route
The Vue router SHALL include a route at `/setup` that renders the setup guide stepper component.

#### Scenario: Route accessible
- **WHEN** the user navigates to `/setup`
- **THEN** the setup guide stepper is rendered

#### Scenario: Navigation link in header
- **WHEN** the app layout header is rendered
- **THEN** a "Setup" navigation link to `/setup` is present

### Requirement: Setup guide step 1 -- self-check
The first step of the setup guide SHALL automatically run the health check (`GET /api/health/setup`) and display all check results. Critical failures (any check with `"fail"` status) SHALL block progression to step 2. The step SHALL show inline fix hints for each failure.

#### Scenario: All checks pass
- **WHEN** step 1 loads and all checks return `"ok"` or `"warn"`
- **THEN** the "Next" button is enabled

#### Scenario: Critical failure blocks progress
- **WHEN** step 1 loads and at least one check returns `"fail"`
- **THEN** the "Next" button is disabled and the failure message is shown with a fix hint

#### Scenario: Re-check button
- **WHEN** the user clicks "Re-check" after fixing an issue
- **THEN** the health check runs again and the results are updated

### Requirement: Setup guide step 2 -- service selection
The second step SHALL display checkboxes for three services: Prowlarr, Sonarr, and Radarr. At least one service SHALL be selected to proceed. Each checkbox SHALL include a brief description of what the service does in the *arr context.

#### Scenario: Default state
- **WHEN** step 2 is displayed
- **THEN** all three checkboxes are unchecked and "Next" is disabled

#### Scenario: Selection enables progress
- **WHEN** the user checks at least one service
- **THEN** the "Next" button is enabled

#### Scenario: Service descriptions shown
- **WHEN** step 2 is displayed
- **THEN** Prowlarr is described as "Indexer Manager", Sonarr as "TV Series", and Radarr as "Movies"

### Requirement: Prowlarr configuration step
When Prowlarr is selected, the guide SHALL show a configuration step with the exact field values for adding FunkArr as a Custom Newznab indexer in Prowlarr. Fields SHALL match Prowlarr's "Add Indexer" UI.

The step SHALL display:
- **Name**: `FunkArr`
- **URL**: `http://<funkarr-host>:<port>` (placeholder)
- **API Path**: `/index/api`
- **API Key**: the configured API key with a copy-to-clipboard button
- **Categories**: `5000 (TV)`, `2000 (Movies)`

Each field SHALL have a copy-to-clipboard button for its value. The step SHALL include a brief instruction telling the user to use Prowlarr's built-in "Test" button to verify the connection.

#### Scenario: Field values displayed
- **WHEN** the Prowlarr step is rendered
- **THEN** all fields are displayed with their values and copy buttons

#### Scenario: API key from health check
- **WHEN** the Prowlarr step is rendered
- **THEN** the API key shown matches the `apiKey.value` from the health check response

#### Scenario: Copy to clipboard
- **WHEN** the user clicks the copy button next to API Path
- **THEN** `/index/api` is copied to the clipboard

### Requirement: Sonarr configuration step
When Sonarr is selected, the guide SHALL show a configuration step with the exact field values for adding FunkArr as a SABnzbd download client in Sonarr. Fields SHALL match Sonarr's "Add Download Client" UI for SABnzbd.

The step SHALL display:
- **Name**: `FunkArr`
- **Host**: `<funkarr-host>` (placeholder)
- **Port**: `<funkarr-port>` (placeholder)
- **URL Base**: `/download/api`
- **API Key**: the configured API key with a copy-to-clipboard button
- **Category**: `tv`

Each field SHALL have a copy-to-clipboard button. The step SHALL instruct the user to use Sonarr's built-in "Test" button.

#### Scenario: Field values displayed
- **WHEN** the Sonarr step is rendered
- **THEN** all fields are displayed with their values and copy buttons

#### Scenario: URL Base value
- **WHEN** the Sonarr step is rendered
- **THEN** the URL Base field shows `/download/api`

### Requirement: Radarr configuration step
When Radarr is selected, the guide SHALL show a configuration step with the exact field values for adding FunkArr as a SABnzbd download client in Radarr. Fields SHALL match Radarr's "Add Download Client" UI for SABnzbd.

The step SHALL display:
- **Name**: `FunkArr`
- **Host**: `<funkarr-host>` (placeholder)
- **Port**: `<funkarr-port>` (placeholder)
- **URL Base**: `/download/api`
- **API Key**: the configured API key with a copy-to-clipboard button
- **Category**: `movies`

Each field SHALL have a copy-to-clipboard button. The step SHALL instruct the user to use Radarr's built-in "Test" button.

#### Scenario: Field values displayed
- **WHEN** the Radarr step is rendered
- **THEN** all fields are displayed with their values and copy buttons

#### Scenario: Category differs from Sonarr
- **WHEN** the Radarr step is rendered
- **THEN** the Category field shows `movies` (not `tv`)

### Requirement: Stepper navigation
The setup guide SHALL provide back/next navigation between steps. The first step SHALL have no "Back" button. The final step SHALL show a "Done" button that navigates to the dashboard. Step indicators SHALL show progress through the guide.

#### Scenario: Linear navigation
- **WHEN** the user is on step 2 and clicks "Next"
- **THEN** step 3 is displayed (first selected service)

#### Scenario: Back navigation
- **WHEN** the user is on a service configuration step and clicks "Back"
- **THEN** the previous step is displayed

#### Scenario: Completion
- **WHEN** the user clicks "Done" on the final service step
- **THEN** the user is navigated to `/` (dashboard)
