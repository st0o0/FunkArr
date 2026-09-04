## Purpose

Internal REST API endpoints for querying ruleset data and scoring history, consumed by the Vue frontend.

## Requirements

### Requirement: List rulesets endpoint
The system SHALL expose `GET /api/rulesets` that returns a JSON array of all registered rulesets. Each entry SHALL contain `ruleSetId`, `topic`, `aliases`, `tvdbId`, `imdbId`, and `tmdbId`. The endpoint SHALL use `TypedResults.Ok()` to return the response, enabling OpenAPI schema inference.

#### Scenario: List with registered rulesets
- **WHEN** `GET /api/rulesets` is called and 3 rulesets are registered
- **THEN** the response is 200 with a JSON array of 3 entries containing identity data

#### Scenario: List with no rulesets
- **WHEN** `GET /api/rulesets` is called and no rulesets are registered
- **THEN** the response is 200 with an empty JSON array

#### Scenario: Actor timeout
- **WHEN** `GET /api/rulesets` is called and the Resolver does not respond within the timeout
- **THEN** the response is 504 with a Problem Details body containing title "Gateway Timeout"

### Requirement: RuleSet detail endpoint
The system SHALL expose `GET /api/rulesets/{id}` that returns the full detail for a single ruleset. The endpoint SHALL use `TypedResults` for all response paths.

#### Scenario: Detail for existing ruleset
- **WHEN** `GET /api/rulesets/tatort` is called and the ruleset exists
- **THEN** the response is 200 with JSON containing identity, source info, default confidence, and rules array

#### Scenario: Detail for unknown ruleset
- **WHEN** `GET /api/rulesets/nonexistent` is called and the ruleSetId is not known
- **THEN** the response is 404 with a Problem Details body

#### Scenario: Actor timeout
- **WHEN** `GET /api/rulesets/{id}` is called and the Manager does not respond within the timeout
- **THEN** the response is 504 with a Problem Details body containing title "Gateway Timeout"

### Requirement: Scoring history endpoint
The system SHALL expose `GET /api/rulesets/{id}/history` with paginated scoring history. The endpoint SHALL use `TypedResults` for all response paths.

#### Scenario: History with results
- **WHEN** `GET /api/rulesets/tatort/history` is called and 5 scoring snapshots exist
- **THEN** the response is 200 with JSON containing `totalCount` and `snapshots` array

#### Scenario: Actor timeout
- **WHEN** the MatchHistoryWorker does not respond within the timeout
- **THEN** the response is 504 with a Problem Details body containing title "Gateway Timeout"

### Requirement: Scoring detail endpoint
The system SHALL expose `GET /api/rulesets/{id}/history/{requestId}` with full scoring detail. The endpoint SHALL use `TypedResults` for all response paths.

#### Scenario: Detail for existing scoring run
- **WHEN** the scoring run exists
- **THEN** the response is 200 with JSON containing scoring detail

#### Scenario: Detail for unknown scoring run
- **WHEN** the requestId is not found
- **THEN** the response is 404 with a Problem Details body

#### Scenario: Actor timeout
- **WHEN** the MatchHistoryWorker does not respond within the timeout
- **THEN** the response is 504 with a Problem Details body containing title "Gateway Timeout"

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
