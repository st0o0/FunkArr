## ADDED Requirements

### Requirement: Setup problem banner in app header
The `App.vue` shell SHALL render a warning banner below the header navigation when the setup status indicates problems (red or amber dot color). The banner SHALL link directly to the setup wizard.

#### Scenario: FFmpeg not found
- **WHEN** the polled status shows `ffmpeg.found === false`
- **THEN** a red banner SHALL appear with text "FFmpeg not found. Run Setup to fix." and a link to `/#/setup`

#### Scenario: Paths not writable
- **WHEN** the polled status shows `paths.downloadOk === false` or `paths.tempOk === false`
- **THEN** an amber banner SHALL appear with text "Download paths not writable. Check Setup." and a link to `/#/setup`

#### Scenario: MediathekViewWeb unreachable
- **WHEN** the polled status shows `mediathek.reachable === false`
- **THEN** an amber banner SHALL appear with text "MediathekViewWeb unreachable. Check connectivity." and a link to `/#/setup`

#### Scenario: API key not configured
- **WHEN** the polled status shows `configured === false`
- **THEN** a red banner SHALL appear with text "API key not configured. Complete Setup." and a link to `/#/setup`

#### Scenario: All checks pass
- **WHEN** the polled status shows all checks passing (green dot)
- **THEN** no banner SHALL be rendered

### Requirement: Banner dismissibility
The banner SHALL be dismissible per browser session. Dismissal state SHALL NOT be persisted to localStorage — the banner reappears on page reload if problems persist.

#### Scenario: User dismisses banner
- **WHEN** the user clicks the dismiss button on the banner
- **THEN** the banner SHALL hide for the remainder of the browser session

#### Scenario: Banner reappears after reload
- **WHEN** the user reloads the page and setup problems still exist
- **THEN** the banner SHALL appear again

### Requirement: Banner not shown on setup page
The banner SHALL NOT be shown when the user is already on the `/setup` route, consistent with the existing header hiding behavior.

#### Scenario: On setup page
- **WHEN** the user is on `/#/setup`
- **THEN** the setup problem banner SHALL NOT be rendered
