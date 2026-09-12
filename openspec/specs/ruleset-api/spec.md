## Purpose

Internal REST API endpoints for querying ruleset data and scoring history, consumed by the Vue frontend.
## Requirements
### Requirement: List rulesets endpoint
The system SHALL expose `GET /api/rulesets` that returns a JSON array of all registered rulesets. Each entry SHALL contain `ruleSetId`, `topic`, `aliases`, `tvdbId`, `imdbId`, `tmdbId`, `mediaName`, `ruleCount`, `sourceType`, `lastScoringRun`, and `matchRate`. The endpoint SHALL gather data from the RuleSetResolver (identity + media name), RuleSetManager (rule count + source type), and MatchHistoryWorker (scoring stats) before assembling the response. The endpoint SHALL use `TypedResults.Ok()` to return the response, enabling OpenAPI schema inference.

#### Scenario: List with registered rulesets
- **WHEN** `GET /api/rulesets` is called and 3 rulesets are registered
- **THEN** the response is 200 with a JSON array of 3 entries containing identity data, media name, rule count, source type, and scoring stats

#### Scenario: List with no rulesets
- **WHEN** `GET /api/rulesets` is called and no rulesets are registered
- **THEN** the response is 200 with an empty JSON array

#### Scenario: Partial data availability
- **WHEN** `GET /api/rulesets` is called and some MatchHistory workers time out
- **THEN** the response is 200 with all rulesets, where timed-out rulesets have `lastScoringRun` and `matchRate` as `null`

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

### Requirement: Endpoint file structure
All ruleset API endpoints (`GET /api/rulesets`, `GET /api/rulesets/{id}`, `GET /api/rulesets/{id}/history`, `GET /api/rulesets/{id}/history/{requestId}`, `POST /api/rulesets`, `PUT /api/rulesets/{id}`, `DELETE /api/rulesets/{id}`, `GET /api/rulesets/{id}/raw`, `POST /api/rulesets/test`) SHALL be registered in a single `RuleSetApiEndpoints` class with one `MapRuleSetApi()` extension method and one `MapGroup("/api/rulesets").WithTags("Rulesets")` call.

#### Scenario: Single route group registration
- **WHEN** the application starts
- **THEN** all `/api/rulesets` endpoints SHALL be registered through a single `MapGroup` call in `RuleSetApiEndpoints.MapRuleSetApi()`

#### Scenario: Endpoint registration
- **WHEN** `ApplicationSetupContainer.SetupApplication` runs
- **THEN** `app.MapRuleSetApi()` is called to register all ruleset read, write, and test endpoints
- **AND** `app.MapMediathekApi()` is called to register the mediathek proxy endpoints

#### Scenario: No authentication on internal API
- **WHEN** any `/api/rulesets` or `/api/mediathek` endpoint is called without an API key
- **THEN** the response is successful (no authentication required on internal API)

#### Scenario: Test endpoint is under rulesets group
- **WHEN** `POST /api/rulesets/test` is called
- **THEN** the request is routed to the ad-hoc scoring test handler

### Requirement: Test request parsing extraction
The JSON parsing logic for the test scoring endpoint (`ParseTestRequest`, `ParseRules`, `ParseIdentification`, `ParseTitleRules`, `ParseFilterSpec`, `ParseFilterNodes`, `ParseFilterCondition`, `ParseFilterField`, `ParseFilterOp`, `ParseCandidates`) SHALL reside in a separate `RuleSetTestRequestParser` static class.

#### Scenario: Parser class independence
- **WHEN** the test endpoint receives a request body
- **THEN** it SHALL delegate JSON parsing to `RuleSetTestRequestParser.Parse()` and receive a parsed result or null

