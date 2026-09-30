## MODIFIED Requirements

### Requirement: Validate on create
`POST /api/rulesets` SHALL accept a typed `CreateRuleSetRequest` model via STJ deserialization. The endpoint SHALL Ask the `LocalRuleSetWriter` actor with a `CreateLocalRuleSet` message and map the response to HTTP status codes: `CreateLocalRuleSetCompleted` → 201, `AlreadyExists` → 409, `ValidationFailed` → 422. The endpoint SHALL NOT directly use `IDataFiles`, `IRuleSetValidator`, or `_diskJsonOptions`.

#### Scenario: Valid create
- **WHEN** a valid `CreateRuleSetRequest` is posted with `ruleSetId` and `topic`
- **THEN** the endpoint SHALL convert the request to a `CreateLocalRuleSet` message, Ask the `LocalRuleSetWriter`, and return 201 on success

#### Scenario: Duplicate ruleSetId
- **WHEN** a `CreateRuleSetRequest` is posted and the writer responds with `AlreadyExists`
- **THEN** the response SHALL be 409 Conflict

#### Scenario: Schema validation failure
- **WHEN** a `CreateRuleSetRequest` is posted and the writer responds with `ValidationFailed`
- **THEN** the response SHALL be 422 with a `ValidationErrorResponse` body

### Requirement: Validate on update
`PUT /api/rulesets/{id}` SHALL accept a typed `UpdateRuleSetRequest` model via STJ deserialization. The endpoint SHALL Ask the `LocalRuleSetWriter` actor with an `UpdateLocalRuleSet` message and map the response to HTTP status codes: `UpdateLocalRuleSetCompleted` → 200, `NotFound` → 404, `ValidationFailed` → 422. The endpoint SHALL NOT directly use `IDataFiles`, `IRuleSetValidator`, or `_diskJsonOptions`.

#### Scenario: Valid update
- **WHEN** a valid `UpdateRuleSetRequest` is put
- **THEN** the endpoint SHALL convert the request to an `UpdateLocalRuleSet` message, Ask the `LocalRuleSetWriter`, and return 200 on success

#### Scenario: Unknown ruleset
- **WHEN** an `UpdateRuleSetRequest` is put for a nonexistent ruleSetId
- **THEN** the response SHALL be 404

#### Scenario: Schema validation failure
- **WHEN** an `UpdateRuleSetRequest` fails validation in the writer
- **THEN** the response SHALL be 422 with a `ValidationErrorResponse` body

### Requirement: Export endpoint
`GET /api/rulesets/{id}/export` SHALL Ask the `LocalRuleSetWriter` actor with an `ExportRuleSet` message and return the flattened JSON on success or 404 if no local component exists. The endpoint SHALL NOT directly read files from disk.

#### Scenario: Export existing local ruleset
- **WHEN** `GET /api/rulesets/tatort/export` is called and a local component exists
- **THEN** the endpoint SHALL Ask the `LocalRuleSetWriter` and return 200 with the flattened JSON

#### Scenario: Export community-only ruleset
- **WHEN** `GET /api/rulesets/community-only/export` is called and no local file exists
- **THEN** the response SHALL be 404

### Requirement: Endpoint file structure
All ruleset API endpoints SHALL be registered in a single `RuleSetApiEndpoints` class. The class SHALL NOT contain `_diskJsonOptions`, `SerializeForDisk` methods, or direct `IDataFiles`/`IRuleSetValidator` injections. Write and export operations SHALL be delegated to the `LocalRuleSetWriter` actor via Ask.

#### Scenario: No disk serialization in API layer
- **WHEN** `RuleSetApiEndpoints.cs` is inspected
- **THEN** it SHALL NOT contain `_diskJsonOptions`, `SerializeForDisk`, `JsonStringEnumConverter`, or direct `IDataFiles.WriteAtomic` calls

#### Scenario: No JsonStringEnumMemberName on API enums
- **WHEN** `FunkArr.Api.Models.Enums.cs` is inspected
- **THEN** no enum members SHALL have `JsonStringEnumMemberName` attributes
