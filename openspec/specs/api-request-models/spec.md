## Purpose

Typed request models for the internal REST API, replacing raw `JsonElement` parsing with STJ-deserialized records for create, update, and test endpoints.

## Requirements

### Requirement: CreateRuleSetRequest typed model
`CreateRuleSetRequest` SHALL be a sealed record implementing `IRuleSetBody`. The record SHALL include DataAnnotation attributes: `[Required]` on `RuleSetId`, `Topic`; `[RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$")]` on `RuleSetId`; `[Required]` and `[MinLength(1)]` on `Rules`. `Confidence` SHALL have `[Range(0.0, 1.0)]` when provided.

#### Scenario: Typed deserialization
- **WHEN** the API receives a Create request with all fields
- **THEN** ASP.NET model binding deserializes into the typed record with enum fields resolved

#### Scenario: Missing required field returns 400
- **WHEN** a POST request omits `Topic`
- **THEN** the API returns 400 before the handler executes

#### Scenario: Invalid RuleSetId format
- **WHEN** a POST request includes `RuleSetId = "Not Kebab"`
- **THEN** the API returns 400 with a regex validation error

#### Scenario: Serialization for disk
- **WHEN** the typed request is serialized to JSON for disk storage
- **THEN** the output matches the ruleset JSON schema (camelCase, no nulls, indented)

#### Scenario: OpenAPI shows request schema
- **WHEN** the OpenAPI spec is generated
- **THEN** the Create endpoint shows the full request body schema with all fields and types

### Requirement: UpdateRuleSetRequest typed model
The `UpdateRuleSetRequest` SHALL be a fully typed record identical to `CreateRuleSetRequest` but without `RuleSetId` (the ID comes from the route parameter).

#### Scenario: Update uses route ID
- **WHEN** a PUT request to `/api/rulesets/{id}` is received
- **THEN** the body is deserialized as `UpdateRuleSetRequest` and the ID comes from the route

#### Scenario: Schema validation still applies
- **WHEN** the deserialized body is re-serialized for `IRuleSetValidator.Validate()`
- **THEN** the validator SHALL receive the same JSON structure as before (field names, nesting)

### Requirement: TestScoreRequest uses shared RuleInput
`TestScoreRequest` SHALL be a sealed record. `DefaultConfidence` SHALL have `[Range(0.0, 1.0)]`. `Rules` SHALL have `[Required]` and `[MinLength(1)]`. `Candidates` SHALL have `[Required]` and `[MinLength(1)]`.

#### Scenario: TestScore reuses RuleInput
- **WHEN** a test score request is received
- **THEN** the rules array deserializes using the same `RuleInput` type as Create/Update

#### Scenario: Empty candidates array
- **WHEN** a POST request to `/api/rulesets/test` includes an empty `Candidates` array
- **THEN** the API returns 400

### Requirement: No manual field extraction in HandleCreate
The `HandleCreate` endpoint SHALL NOT use `request.Body.TryGetValue(...)` for field extraction. Required field validation SHALL be handled by ASP.NET model binding.

#### Scenario: No TryGetValue in HandleCreate
- **WHEN** `HandleCreate` is inspected
- **THEN** no `TryGetValue` calls exist

### Requirement: FilterNode polymorphic deserialization
The `FilterSpec` model with `All`, `Any`, `Not` arrays of `FilterNode` SHALL deserialize correctly using a custom `JsonConverter<FilterNode>`. A JSON object containing `all`, `any`, or `not` properties SHALL deserialize as a `FilterNode.GroupNode`; otherwise it SHALL deserialize as a `FilterNode.ConditionNode`.

#### Scenario: Condition node deserialization
- **WHEN** a filter array element has `field`, `op`, and `value` properties
- **THEN** it SHALL deserialize as `FilterNode.ConditionNode` with a `FilterCondition`

#### Scenario: Group node deserialization
- **WHEN** a filter array element has `all`, `any`, or `not` properties
- **THEN** it SHALL deserialize as `FilterNode.GroupNode` with a nested `FilterSpec`

#### Scenario: Nested groups
- **WHEN** a filter contains groups nested 3 levels deep
- **THEN** the `JsonConverter` SHALL recursively deserialize all levels correctly

### Requirement: MediathekSearchRequest typed request record

`MediathekSearchRequest` SHALL be a sealed record with `[FromQuery]` attributes on all properties. The `MediathekApiEndpoints` search endpoint SHALL bind this record via `[AsParameters]` instead of individual lambda parameters. The record SHALL include `[Range]` attributes: `Limit` range 1–100, `Offset` minimum 0, `DurationMin` minimum 0, `DurationMax` minimum 0.

#### Scenario: Search endpoint binds via AsParameters

- **WHEN** a GET request to `/api/mediathek/search?q=test&limit=10` is received
- **THEN** the endpoint handler receives a bound `MediathekSearchRequest` instance with `Q = "test"` and `Limit = 10`

#### Scenario: Out of range limit rejected

- **WHEN** a GET request includes `limit=-5`
- **THEN** the API returns 400 with a validation error on Limit

#### Scenario: OpenAPI shows query parameters

- **WHEN** the OpenAPI spec is generated for the search endpoint
- **THEN** all 9 query parameters SHALL appear with their types

#### Scenario: Validation logic stays in handler

- **WHEN** all three of Q, Channel, and Topic are null or whitespace
- **THEN** the handler SHALL return 400 Bad Request (validation remains in handler code, not on the model)

### Requirement: DownloadHistoryRequest typed request record

`DownloadHistoryRequest` SHALL be a sealed record with `[FromQuery]` attributes on all properties. The `DownloadsApiEndpoints` history endpoint SHALL bind this record via `[AsParameters]` instead of individual lambda parameters. The record SHALL include `[Range]` attributes: `Start` minimum 0, `Limit` range 1–1000.

#### Scenario: History endpoint binds via AsParameters

- **WHEN** a GET request to `/api/downloads/history?start=0&limit=50` is received
- **THEN** the endpoint handler receives a bound `DownloadHistoryRequest` instance

#### Scenario: Default values applied in handler

- **WHEN** Start and Limit are null
- **THEN** the handler SHALL apply defaults (Start=0, Limit=25) - defaulting stays in handler code

### Requirement: CreateArrResourceRequest validated
`CreateArrResourceRequest` SHALL include `[Required]` on `Url` and `ApiKey`, and `[SafeUrl]` on `Url` to prevent SSRF.

#### Scenario: Missing API key in setup request
- **WHEN** a POST request to `/api/setup/sonarr/indexer` omits `ApiKey`
- **THEN** the API returns 400

#### Scenario: SSRF attempt blocked
- **WHEN** a POST request includes `Url = "http://169.254.169.254/metadata"`
- **THEN** the API returns 400 with SafeUrl validation error

### Requirement: RuleSetApiEndpoints list handler extracted

The RuleSetApiEndpoints list endpoint logic SHALL be extracted into a `private static` method (e.g., `HandleList`) matching the pattern of existing `HandleCreate`, `HandleUpdate`, `HandleDelete`, and `HandleExport` methods.

#### Scenario: List handler is a named method

- **WHEN** reviewing RuleSetApiEndpoints code
- **THEN** the list endpoint SHALL delegate to a `private static` method, not contain inline fan-out logic in the lambda

### Requirement: Single SerializeForDisk method

RuleSetApiEndpoints SHALL have exactly one `SerializeForDisk` helper method, not duplicated implementations.

#### Scenario: No duplicate serialization methods

- **WHEN** reviewing RuleSetApiEndpoints code
- **THEN** there SHALL be exactly one `SerializeForDisk` method covering both Create and Update serialization needs
