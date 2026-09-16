## Purpose

Typed response models for the internal REST API, replacing anonymous objects with named records for consistent serialization and OpenAPI schema generation.

## Requirements

### Requirement: Operation result model
The system SHALL use a typed `OperationResult` record with `Success` (bool) and `Error` (string, nullable) for all mutation endpoint responses that currently use anonymous `new { success, error }` objects. This applies to `DELETE /api/downloads/queue/{id}`, `DELETE /api/downloads/history/{id}`, and `POST /api/downloads/{id}/retry`.

#### Scenario: Successful operation
- **WHEN** a mutation endpoint succeeds
- **THEN** the response body SHALL be `{"success":true,"error":null}` (or `error` omitted per STJ null handling)

#### Scenario: Failed operation
- **WHEN** a mutation endpoint fails with an error message
- **THEN** the response body SHALL be `{"success":false,"error":"<message>"}`

#### Scenario: Wire format compatibility
- **WHEN** the typed `OperationResult` is serialized with camelCase policy
- **THEN** the JSON output SHALL be identical to the previous anonymous object format

### Requirement: Test score response model
The system SHALL use a typed `TestScoreResponse` record with `ItemTraces` (array of `ItemTrace`) for the `POST /api/rulesets/test` response, replacing the anonymous `new { itemTraces = ... }` object.

#### Scenario: Test response serialization
- **WHEN** the test endpoint returns a successful response
- **THEN** the body SHALL contain `{"itemTraces":[...]}` matching the existing format

#### Scenario: OpenAPI schema
- **WHEN** the OpenAPI spec is generated
- **THEN** the test endpoint SHALL declare `Produces<TestScoreResponse>()` with a fully typed schema

### Requirement: Error response model
The system SHALL use a typed `ErrorResponse` record with `Error` (string) for endpoints that return `new { error = "..." }`. A typed `ValidationErrorResponse` record with `Errors` (list) SHALL be used for validation failure responses that return `new { errors = [...] }`.

#### Scenario: Single error response
- **WHEN** an endpoint returns a bad request with an error message
- **THEN** the body SHALL be `{"error":"<message>"}` using the `ErrorResponse` type

#### Scenario: Validation error response
- **WHEN** a create or update endpoint fails schema validation
- **THEN** the body SHALL be `{"errors":[...]}` using the `ValidationErrorResponse` type

### Requirement: Mediathek search models in Models directory
The `MediathekSearchResponse` and `MediathekSearchResult` records SHALL reside in `FunkArr.Api/Models/` as public sealed records, consistent with all other API response models.

#### Scenario: Model location
- **WHEN** the `MediathekApiEndpoints` file is inspected
- **THEN** it SHALL NOT contain inline record type definitions
- **AND** the response records SHALL be in `Models/MediathekSearch.cs`

### Requirement: Created response model
The system SHALL use a typed `CreatedRuleSetResponse` record with `RuleSetId` (string) for the `POST /api/rulesets` 201 response, replacing the anonymous `new { ruleSetId }` object.

#### Scenario: Created response serialization
- **WHEN** a ruleset is successfully created
- **THEN** the response body SHALL be `{"ruleSetId":"<id>"}` using the typed model

### Requirement: Date fields use DateTimeOffset
`DownloadHistoryItem.CompletedAt` SHALL be `DateTimeOffset` instead of `string`. `RuleSetListEntry.LastScoringRun` SHALL be `DateTimeOffset?` instead of `string?`. No manual `.ToString("o")` conversion SHALL occur in endpoint code.

#### Scenario: History item has DateTimeOffset
- **WHEN** the API returns a download history item
- **THEN** `completedAt` is serialized as an ISO 8601 DateTimeOffset by System.Text.Json

#### Scenario: No manual date formatting in endpoints
- **WHEN** endpoint code is inspected
- **THEN** no `.ToString("o")` calls exist for date fields

### Requirement: Unified registry access
All endpoint classes SHALL use `await registry.GetAsync<T>()` for actor resolution. No synchronous `registry.Get<T>()` calls SHALL exist in endpoint code.

#### Scenario: Downloads endpoints use GetAsync
- **WHEN** `DownloadsApiEndpoints.cs` is inspected
- **THEN** all actor resolution uses `await registry.GetAsync<T>()`
