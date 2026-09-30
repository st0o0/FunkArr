## ADDED Requirements

### Requirement: RuleSet API endpoints have contract tests
Every RuleSet API endpoint SHALL have at least one integration test with realistic data and typed response deserialization.

#### Scenario: List rulesets with entries
- **WHEN** `GET /api/rulesets` is requested and probes respond with ruleset data
- **THEN** the typed `RuleSetListResponse` SHALL contain entries with correct names and match rates

#### Scenario: Get ruleset detail
- **WHEN** `GET /api/rulesets/{id}` is requested and the probe responds with detail
- **THEN** the typed response SHALL contain identity, enrichment config, and rules

#### Scenario: Create ruleset returns 201
- **WHEN** `POST /api/rulesets` is sent with valid body and the probe responds with success
- **THEN** the response SHALL be 201 with `CreatedRuleSetResponse` containing the ruleset ID

#### Scenario: Create with validation errors returns 422
- **WHEN** the probe responds with validation failure
- **THEN** the response SHALL be 422 with `ValidationErrorResponse` containing error messages

#### Scenario: Test scoring returns traces
- **WHEN** `POST /api/rulesets/test` is sent and the probe responds with scoring results
- **THEN** the typed `TestScoreResponse` SHALL contain item traces with rule outcomes
