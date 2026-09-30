## MODIFIED Requirements

### Requirement: RuleSet detail endpoint
The system SHALL expose `GET /api/rulesets/{id}` that returns the full detail for a single ruleset. The endpoint SHALL use `TypedResults` for all response paths. Each rule in the response SHALL include structured, typed fields: `strategy` as the JSON wire enum name (e.g. `"seasonAndEpisodeNumber"`, `"itemTitleExact"`), `filters` as a structured filter group (with `all`/`any`/`not` arrays of typed conditions), `titleRules` as a structured array of typed title parts, and `seasonRegex`/`episodeRegex`/`captureGroup` as direct fields. The response SHALL NOT include pre-formatted string fields `matchMode`, `filterSummary`, or pre-formatted `titleParts` string arrays.

#### Scenario: Detail for existing ruleset
- **WHEN** `GET /api/rulesets/tatort` is called and the ruleset exists
- **THEN** the response is 200 with JSON containing identity, source info, default confidence, and rules array with structured typed fields

#### Scenario: Detail rule strategy as wire name
- **WHEN** a rule uses `IdentificationStrategy.TitleExact`
- **THEN** the detail response SHALL serialize strategy as `"itemTitleExact"` (JSON wire name), not `"TitleExact"` (C# enum name)

#### Scenario: Detail rule with structured filters
- **WHEN** a rule has filter conditions `all: [{ field: "title", op: "contains", value: "Tatort" }]`
- **THEN** the detail response SHALL include `filters: { all: [{ field: "title", op: "contains", value: "Tatort" }] }` as structured data, not a summary string

#### Scenario: Detail rule with structured title rules
- **WHEN** a rule uses `itemTitleExact` strategy with title rules
- **THEN** the detail response SHALL include `titleRules: [{ type: "regex", field: "title", pattern: "...", captureGroup: 1 }]` as structured data

#### Scenario: Detail rule without matchMode field
- **WHEN** any rule is returned in the detail response
- **THEN** the response SHALL NOT contain a `matchMode` field

#### Scenario: Detail for unknown ruleset
- **WHEN** `GET /api/rulesets/nonexistent` is called and the ruleSetId is not known
- **THEN** the response is 404 with a Problem Details body

#### Scenario: Actor timeout
- **WHEN** `GET /api/rulesets/{id}` is called and the Manager does not respond within the timeout
- **THEN** the response is 504 with a Problem Details body containing title "Gateway Timeout"

### Requirement: Endpoint file structure
All ruleset API endpoints (`GET /api/rulesets`, `GET /api/rulesets/{id}`, `GET /api/rulesets/{id}/history`, `GET /api/rulesets/{id}/history/{requestId}`, `POST /api/rulesets`, `PUT /api/rulesets/{id}`, `DELETE /api/rulesets/{id}`, `POST /api/rulesets/test`, `GET /api/rulesets/{id}/export`) SHALL be registered in a single `RuleSetApiEndpoints` class with one `MapRuleSetApi()` extension method and one `MapGroup("/api/rulesets").WithTags("Rulesets")` call.

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

## REMOVED Requirements

### Requirement: Raw endpoint
**Reason**: The Detail endpoint now returns structured typed data that serves the same purpose as the raw JSON pass-through. The Builder can use the Detail endpoint directly.
**Migration**: Replace `GET /api/rulesets/{id}/raw` calls with `GET /api/rulesets/{id}`. The response shape is structured but contains the same fields the Builder needs.
