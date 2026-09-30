## MODIFIED Requirements

### Requirement: API endpoint group and routing
All ruleset API endpoints SHALL be registered under a `/api/rulesets` route group in `FunkArr.Api`. The Api project SHALL expose a `MapRuleSetApi(this WebApplication app)` extension method following the same pattern as `MapIndexerApi` and `MapDownloadApi`. The method SHALL register both read endpoints (GET list, GET detail, GET history, GET scoring detail) and write endpoints (POST create, PUT update, DELETE remove). The method SHALL also register the ad-hoc test endpoint (`POST /api/rulesets/test`) and the MediathekViewWeb proxy endpoint (`GET /api/mediathek/search`) under a separate `/api/mediathek` route group via a `MapMediathekApi(this WebApplication app)` extension method.

#### Scenario: Endpoint registration
- **WHEN** `ApplicationSetupContainer.SetupApplication` runs
- **THEN** `app.MapRuleSetApi()` is called to register all ruleset read and write endpoints
- **AND** `app.MapMediathekApi()` is called to register the mediathek proxy endpoints

#### Scenario: No authentication on internal API
- **WHEN** any `/api/rulesets` or `/api/mediathek` endpoint is called without an API key
- **THEN** the response is successful (no authentication required on internal API)

#### Scenario: Test endpoint is under rulesets group
- **WHEN** `POST /api/rulesets/test` is called
- **THEN** the request is routed to the ad-hoc scoring test handler
