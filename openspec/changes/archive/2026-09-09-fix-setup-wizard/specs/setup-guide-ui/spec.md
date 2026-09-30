# setup-guide-ui (delta)

## MODIFIED Requirements

### Requirement: Service configuration displays connection details

The Setup Guide service config steps SHALL display pre-filled connection details based on the current browser location and health check data. URL Base values SHALL match how each *arr application constructs the full API URL.

#### Scenario: Prowlarr config shows correct values

- **WHEN** the Prowlarr configuration step is displayed
- **THEN** the URL field SHALL show `http://<actual-hostname>:<actual-port>` derived from the browser location
- **AND** the API Path SHALL be `/index/api`
- **AND** the API Key SHALL be the actual key from health check

#### Scenario: Sonarr config shows correct URL Base

- **WHEN** the Sonarr SABnzbd configuration step is displayed
- **THEN** the Host field SHALL show the actual hostname from browser location
- **AND** the Port field SHALL show the actual port from browser location
- **AND** the URL Base SHALL be `/download` (NOT `/download/api`, because Sonarr appends `/api` itself)

#### Scenario: Radarr config shows correct URL Base

- **WHEN** the Radarr SABnzbd configuration step is displayed
- **THEN** the URL Base SHALL be `/download` (NOT `/download/api`)
- **AND** the Host and Port SHALL match Sonarr's pattern

#### Scenario: Non-standard port

- **WHEN** FunkArr is accessed at `http://myserver:8080`
- **THEN** the config steps SHALL show `myserver` as host and `8080` as port
