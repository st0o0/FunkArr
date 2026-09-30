## ADDED Requirements

### Requirement: Header status indicator
The header bar SHALL display a gear icon with an adjacent status dot on the right side. The dot SHALL reflect system health by polling `GET /api/v1/setup/status` every 30 seconds.

#### Scenario: All systems healthy
- **WHEN** the setup status response has `configured=true`, `ffmpeg.found=true`, `paths.downloadOk=true`, `paths.tempOk=true`, and `mediathek.reachable=true`
- **THEN** the status dot SHALL be green

#### Scenario: Critical failure
- **WHEN** the setup status response has `configured=false` or `ffmpeg.found=false`
- **THEN** the status dot SHALL be red

#### Scenario: Degraded state
- **WHEN** the setup status response has all critical checks passing but one or more of `paths.downloadOk`, `paths.tempOk`, or `mediathek.reachable` is `false`
- **THEN** the status dot SHALL be amber

#### Scenario: Navigate to settings
- **WHEN** the user clicks the gear icon
- **THEN** the app SHALL navigate to `/settings`

#### Scenario: Polling interval
- **WHEN** the app is running
- **THEN** the status SHALL be refreshed every 30 seconds without blocking the UI

#### Scenario: Initial load
- **WHEN** the app starts and no status response has been received yet
- **THEN** the status dot SHALL not be displayed until the first response arrives
