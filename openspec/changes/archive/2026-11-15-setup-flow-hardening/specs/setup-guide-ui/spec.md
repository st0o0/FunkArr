## Purpose

Delta spec for setup guide UI — pre-fills FunkArr URL from health check, adds Prowlarr topology guidance, fixes dead code.

## ADDED Requirements

### Requirement: Prowlarr topology guidance
When Prowlarr is selected in the service selection step (step 2), the Sonarr and Radarr configuration steps SHALL hide the "Create Indexer" button and display an info callout explaining that Prowlarr syncs the indexer automatically. Only the "Create Download Client" button SHALL be shown.

#### Scenario: Prowlarr selected hides Sonarr indexer button
- **WHEN** Prowlarr and Sonarr are both selected in step 2
- **AND** the user navigates to the Sonarr configuration step
- **THEN** the "Create Indexer" button SHALL NOT be displayed
- **AND** an info callout SHALL display "Prowlarr syncs the indexer to Sonarr automatically. Only the download client is needed here."

#### Scenario: Prowlarr selected hides Radarr indexer button
- **WHEN** Prowlarr and Radarr are both selected in step 2
- **AND** the user navigates to the Radarr configuration step
- **THEN** the "Create Indexer" button SHALL NOT be displayed
- **AND** an info callout SHALL be displayed

#### Scenario: Without Prowlarr both buttons shown
- **WHEN** only Sonarr is selected (Prowlarr not selected)
- **AND** the user navigates to the Sonarr configuration step
- **THEN** both "Create Indexer" and "Create Download Client" buttons SHALL be displayed

#### Scenario: Prowlarr step unaffected
- **WHEN** the user is on the Prowlarr configuration step
- **THEN** the "Create Indexer" button SHALL be shown as before (Prowlarr always needs it)

### Requirement: Prowlarr downloadClient handler cleanup
The service action map SHALL NOT assign a `downloadClient` handler to Prowlarr. The current mapping of `createProwlarrIndexer` as Prowlarr's `downloadClient` function SHALL be removed.

#### Scenario: Prowlarr has no downloadClient function
- **WHEN** the service action map is defined
- **THEN** Prowlarr's `downloadClient` entry SHALL be `undefined` or absent

## MODIFIED Requirements

### Requirement: Arr service credential inputs
Each service configuration step (step 3+) SHALL display two input fields above the action buttons: a URL text input (placeholder: `http://sonarr:8989`) and an API Key password input. Both fields SHALL be required before the Create buttons become enabled. The FunkArr URL field SHALL be pre-filled from the health check response `connectionInfo.baseUrl` when available (non-empty), falling back to `window.location` derived values.

#### Scenario: Empty inputs disable buttons
- **WHEN** the service step renders with empty URL and API Key fields
- **THEN** all Create buttons for that service are disabled

#### Scenario: Both fields filled enables buttons
- **WHEN** the user enters both a URL and an API Key
- **THEN** the Create buttons for that service become enabled

#### Scenario: FunkArr URL pre-filled from config
- **WHEN** the health check response `connectionInfo.baseUrl` is `http://funkarr:6969`
- **THEN** the FunkArr URL field SHALL be pre-filled with `http://funkarr:6969`

#### Scenario: FunkArr URL falls back to browser location
- **WHEN** the health check response `connectionInfo.baseUrl` is empty
- **THEN** the FunkArr URL field SHALL derive its value from `window.location` as before

#### Scenario: Manual config shows configured base URL
- **WHEN** `connectionInfo.baseUrl` is `http://funkarr:6969`
- **AND** the user expands "Configure manually"
- **THEN** the manual config host and port fields SHALL reflect the configured base URL values
