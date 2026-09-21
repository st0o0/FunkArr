## Purpose

Internal REST API endpoints for querying ruleset data and scoring history, consumed by the Vue frontend.
## Requirements
### Requirement: List rulesets endpoint
The system SHALL expose `GET /api/rulesets` that returns a JSON object with `communityVersion` (string or null) and `rulesets` (array). The `communityVersion` SHALL be read from `version.txt` via `IDataFiles`. When `version.txt` does not exist, `communityVersion` SHALL be `null`. Each entry in `rulesets` SHALL contain `ruleSetId`, `topic`, `aliases`, `tvdbId`, `imdbId`, `tmdbId`, `mediaName`, `ruleCount`, `sourceType`, `lastScoringRun`, and `matchRate`. The endpoint SHALL gather data from the RuleSetResolver (identity + media name), RuleSetManager (rule count + source type), and ScoringHistoryWorker (scoring stats) before assembling the response. The endpoint SHALL use `TypedResults.Ok()` to return the response, enabling OpenAPI schema inference.

#### Scenario: List with registered rulesets
- **WHEN** `GET /api/rulesets` is called and 3 rulesets are registered and community version is "1.2.0"
- **THEN** the response is 200 with a JSON object containing `communityVersion` "1.2.0" and `rulesets` array of 3 entries with identity data, media name, rule count, source type, and scoring stats

#### Scenario: List with no rulesets
- **WHEN** `GET /api/rulesets` is called and no rulesets are registered
- **THEN** the response is 200 with `communityVersion` and an empty `rulesets` array

#### Scenario: Partial data availability
- **WHEN** `GET /api/rulesets` is called and some ScoringHistory workers time out
- **THEN** the response is 200 with all rulesets in the `rulesets` array, where timed-out rulesets have `lastScoringRun` and `matchRate` as `null`

#### Scenario: Actor timeout
- **WHEN** `GET /api/rulesets` is called and the Resolver does not respond within the timeout
- **THEN** the response is 504 with a Problem Details body containing title "Gateway Timeout"

### Requirement: RuleSet detail endpoint
The system SHALL expose `GET /api/rulesets/{id}` that returns the full detail for a single ruleset. The endpoint SHALL use `TypedResults` for all response paths. Each rule in the response SHALL include structured, typed fields: `strategy` as the JSON wire enum name (e.g. `"seasonAndEpisodeNumber"`, `"itemTitleExact"`), `filters` as a structured filter group (with `all`/`any`/`not` arrays of typed conditions), `titleRules` as a structured array of typed title parts, and `seasonRegex`/`episodeRegex`/`captureGroup` as direct fields. The response SHALL NOT include pre-formatted string fields `matchMode`, `filterSummary`, or pre-formatted `titleParts` string arrays. The response SHALL include enrichment config in the RuleSetDetail response.

#### Scenario: Detail for existing ruleset
- **WHEN** `GET /api/rulesets/tatort` is called and the ruleset exists
- **THEN** the response is 200 with JSON containing identity, source info, default confidence, and rules array with structured typed fields

#### Scenario: Detail with enrichment config
- **WHEN** GET /api/rulesets/{id} is called
- **THEN** the response includes an enrichment object with the ruleset's resolved enrichment config (merged from community + local, or defaults)

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

### Requirement: Scoring history endpoint
The system SHALL expose `GET /api/rulesets/{id}/history` with paginated scoring history. The endpoint SHALL use `TypedResults` for all response paths.

#### Scenario: History with results
- **WHEN** `GET /api/rulesets/tatort/history` is called and 5 scoring snapshots exist
- **THEN** the response is 200 with JSON containing `totalCount` and `snapshots` array

#### Scenario: Actor timeout
- **WHEN** the ScoringHistoryWorker does not respond within the timeout
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
- **WHEN** the ScoringHistoryWorker does not respond within the timeout
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

### Requirement: Test request parsing extraction
The test scoring endpoint (`POST /api/rulesets/test`) SHALL accept a typed `TestScoreRequest` model via STJ deserialization. The system SHALL use a custom `JsonConverter<FilterNode>` for polymorphic filter deserialization. The separate `RuleSetTestRequestParser` class SHALL be removed. Strategy mapping from frontend names to domain types SHALL be handled by a static helper method.

#### Scenario: Request deserialization
- **WHEN** the test endpoint receives a request body
- **THEN** STJ SHALL deserialize it into a `TestScoreRequest` instance automatically
- **AND** the endpoint SHALL map the deserialized model to domain `MatchingConfig` and `ScoreCandidate[]`

#### Scenario: Invalid request body
- **WHEN** the test endpoint receives a body that cannot be deserialized (missing required fields)
- **THEN** the response SHALL be 400 Bad Request

#### Scenario: Parser class removed
- **WHEN** the codebase is inspected
- **THEN** `RuleSetTestRequestParser.cs` SHALL NOT exist

### Requirement: Create endpoint accepts enrichment config
The POST /api/rulesets/ endpoint SHALL accept an optional enrichment object in CreateRuleSetRequest and serialize it to disk.

#### Scenario: Create with enrichment
- **WHEN** POST /api/rulesets/ includes enrichment config
- **THEN** the enrichment object is serialized to the JSON file

### Requirement: Update endpoint accepts enrichment config
The PUT /api/rulesets/{id} endpoint SHALL accept an optional enrichment object in UpdateRuleSetRequest and serialize it to disk.

#### Scenario: Update with enrichment
- **WHEN** PUT /api/rulesets/{id} includes enrichment config
- **THEN** the enrichment object is serialized to the JSON file

### Requirement: Test endpoint accepts enrichment config and identity
The POST /api/rulesets/test endpoint SHALL accept optional enrichment config and identity fields (tvdbId, tmdbId, imdbId, mediaType) alongside existing scoring config.

#### Scenario: Test with enrichment
- **WHEN** POST /api/rulesets/test includes enrichment config and tvdbId
- **THEN** enrichment runs after scoring and results include EnrichmentTrace

#### Scenario: Test without enrichment
- **WHEN** POST /api/rulesets/test omits enrichment config
- **THEN** behavior is unchanged from current (scoring only)

### Requirement: Validate on create
`POST /api/rulesets` SHALL accept a typed `CreateRuleSetRequest` model via STJ deserialization. The endpoint SHALL validate `RuleSetId` format and `Topic` presence from the typed model. The endpoint SHALL re-serialize the body (without `ruleSetId`) to JSON for `IRuleSetValidator.Validate()`.

#### Scenario: Valid create
- **WHEN** a valid `CreateRuleSetRequest` is posted with `ruleSetId` and `topic`
- **THEN** the endpoint SHALL strip `ruleSetId`, serialize the rest, validate via `IRuleSetValidator`, write the file, and return 201

#### Scenario: Invalid ruleSetId format
- **WHEN** a `CreateRuleSetRequest` with non-kebab-case `ruleSetId` is posted
- **THEN** the response SHALL be 400 with an `ErrorResponse` body

#### Scenario: Missing topic
- **WHEN** a `CreateRuleSetRequest` with null or whitespace `topic` is posted
- **THEN** the response SHALL be 400 with an `ErrorResponse` body

#### Scenario: Schema validation failure
- **WHEN** a `CreateRuleSetRequest` fails `IRuleSetValidator.Validate()`
- **THEN** the response SHALL be 422 with a `ValidationErrorResponse` body

### Requirement: Validate on update
`PUT /api/rulesets/{id}` SHALL accept a typed `UpdateRuleSetRequest` model via STJ deserialization. The endpoint SHALL serialize the model to JSON for `IRuleSetValidator.Validate()`.

#### Scenario: Valid update
- **WHEN** a valid `UpdateRuleSetRequest` is put
- **THEN** the endpoint SHALL serialize, validate, write the file, and return 200

#### Scenario: Schema validation failure
- **WHEN** an `UpdateRuleSetRequest` fails `IRuleSetValidator.Validate()`
- **THEN** the response SHALL be 422 with a `ValidationErrorResponse` body

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

