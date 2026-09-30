## MODIFIED Requirements

### Requirement: Endpoint file structure
All ruleset API endpoints (`GET /api/rulesets`, `GET /api/rulesets/{id}`, `GET /api/rulesets/{id}/history`, `GET /api/rulesets/{id}/history/{requestId}`, `POST /api/rulesets`, `PUT /api/rulesets/{id}`, `DELETE /api/rulesets/{id}`, `GET /api/rulesets/{id}/raw`, `POST /api/rulesets/test`, `GET /api/rulesets/{id}/export`) SHALL be registered in a single `RuleSetApiEndpoints` class with one `MapRuleSetApi()` extension method and one `MapGroup("/api/rulesets").WithTags("Rulesets")` call.

#### Scenario: Single route group registration
- **WHEN** the application starts
- **THEN** all `/api/rulesets` endpoints SHALL be registered through a single `MapGroup` call in `RuleSetApiEndpoints.MapRuleSetApi()`

#### Scenario: Endpoint registration
- **WHEN** `ApplicationSetupContainer.SetupApplication` runs
- **THEN** `app.MapRuleSetApi()` is called to register all ruleset read, write, test, and export endpoints
- **AND** `app.MapMediathekApi()` is called to register the mediathek proxy endpoints

#### Scenario: No authentication on internal API
- **WHEN** any `/api/rulesets` or `/api/mediathek` endpoint is called without an API key
- **THEN** the response is successful (no authentication required on internal API)

#### Scenario: Test endpoint is under rulesets group
- **WHEN** `POST /api/rulesets/test` is called
- **THEN** the request is routed to the ad-hoc scoring test handler

## ADDED Requirements

### Requirement: Validate on create
`POST /api/rulesets` SHALL validate the request body against the schema and business rules via `IRuleSetValidator` before writing to disk.

#### Scenario: Valid create
- **WHEN** a valid ruleset JSON is posted
- **THEN** the file is written and 201 is returned

#### Scenario: Invalid create
- **WHEN** an invalid ruleset JSON is posted (e.g. missing topic, invalid strategy)
- **THEN** the response is 422 with a JSON body `{ "errors": [...] }` containing all validation errors
- **AND** no file is written to disk

### Requirement: Validate on update
`PUT /api/rulesets/{id}` SHALL validate the request body against the schema and business rules via `IRuleSetValidator` before writing to disk.

#### Scenario: Valid update
- **WHEN** a valid ruleset JSON is put
- **THEN** the file is updated and 200 is returned

#### Scenario: Invalid update
- **WHEN** an invalid ruleset JSON is put
- **THEN** the response is 422 with a JSON body `{ "errors": [...] }` containing all validation errors
- **AND** the existing file is not modified

### Requirement: Export endpoint
`GET /api/rulesets/{id}/export` SHALL return the flattened, standalone, schema-validated JSON for a ruleset with a local component.

#### Scenario: Export existing local ruleset
- **WHEN** `GET /api/rulesets/my-show/export` is called and `my-show` has a local file
- **THEN** the response is 200 with `Content-Type: application/json` and `Content-Disposition: attachment; filename="my-show.json"`

#### Scenario: Export community-only ruleset
- **WHEN** `GET /api/rulesets/tatort/export` is called and `tatort` has no local file
- **THEN** the response is 404 with a Problem Details body indicating no local ruleset exists to export

#### Scenario: Export unknown ruleset
- **WHEN** `GET /api/rulesets/nonexistent/export` is called
- **THEN** the response is 404

### Requirement: Validation error response format
Validation error responses SHALL use HTTP 422 with a JSON body containing an `errors` array.

#### Scenario: Error response structure
- **WHEN** a save request fails validation with 2 errors
- **THEN** the response body is `{ "errors": [{ "field": "...", "message": "..." }, { "field": "...", "message": "..." }] }`
