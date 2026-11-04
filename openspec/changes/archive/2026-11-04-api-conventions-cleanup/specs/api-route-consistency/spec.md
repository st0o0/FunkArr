## ADDED Requirements

### Requirement: Queue operations nested under /queue/
All queue-related operations in `DownloadsApiEndpoints` SHALL be nested under the `/queue/` path prefix. This includes retry, pause, and resume which currently sit at the wrong nesting level.

#### Scenario: Retry endpoint path
- **WHEN** a POST request is made to retry a download
- **THEN** the path SHALL be `POST /api/downloads/queue/{id}/retry`
- **AND** `POST /api/downloads/{id}/retry` SHALL NOT be a valid route

#### Scenario: Pause endpoint path
- **WHEN** a POST request is made to pause the download queue
- **THEN** the path SHALL be `POST /api/downloads/queue/pause`
- **AND** `POST /api/downloads/pause` SHALL NOT be a valid route

#### Scenario: Resume endpoint path
- **WHEN** a POST request is made to resume the download queue
- **THEN** the path SHALL be `POST /api/downloads/queue/resume`
- **AND** `POST /api/downloads/resume` SHALL NOT be a valid route

### Requirement: Feed root uses forward slash
The feed list endpoint SHALL use `MapGet("/")` instead of `MapGet("")` for consistency with all other endpoint groups.

#### Scenario: Feed root path registration
- **WHEN** `FeedApiEndpoints.cs` is inspected
- **THEN** the root GET endpoint SHALL be registered as `MapGet("/")`

### Requirement: Route path conventions documented
`AGENTS.md` SHALL document the route path conventions: groups use `/api/{feature}`, sub-resources use consistent nesting, actions on a resource use `POST /resource/{id}/action`, collection-level actions use `POST /resource/action`.

#### Scenario: AGENTS.md has route conventions
- **WHEN** `AGENTS.md` is inspected
- **THEN** it SHALL contain route path naming and nesting rules

### Requirement: Frontend updated for route changes
The Vue.js frontend SHALL update API calls for the three changed routes: retry, pause, resume. No other route changes are required.

#### Scenario: Frontend retry call updated
- **WHEN** the frontend retries a download
- **THEN** it SHALL POST to `/api/downloads/queue/{id}/retry`

#### Scenario: Frontend pause call updated
- **WHEN** the frontend pauses downloads
- **THEN** it SHALL POST to `/api/downloads/queue/pause`

#### Scenario: Frontend resume call updated
- **WHEN** the frontend resumes downloads
- **THEN** it SHALL POST to `/api/downloads/queue/resume`
