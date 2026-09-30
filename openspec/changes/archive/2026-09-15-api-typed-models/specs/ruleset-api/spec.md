## MODIFIED Requirements

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
