## MODIFIED Requirements

### Requirement: Endpoint metadata completeness
Every visible internal API endpoint SHALL have `.WithSummary()` and `.WithDescription()`. Every endpoint SHALL declare its full response type surface via `.Produces<T>()` for success responses and `.ProducesProblem()` for each error status code the endpoint can return.

#### Scenario: All endpoints have descriptions
- **WHEN** the OpenAPI spec is generated
- **THEN** every endpoint not marked with `.ExcludeFromDescription()` SHALL have a non-empty description

#### Scenario: Setup health check declares response type
- **WHEN** the `/api/system/setup` endpoint metadata is inspected
- **THEN** it SHALL declare `.Produces<SetupHealthCheck>()`

#### Scenario: Setup provisioning endpoints declare response type
- **WHEN** any `/api/setup/*` endpoint metadata is inspected
- **THEN** it SHALL declare `.Produces<ArrResourceResponse>()`

#### Scenario: RuleSet raw and export declare response types
- **WHEN** `/api/rulesets/{id}/raw` or `/api/rulesets/{id}/export` endpoint metadata is inspected
- **THEN** they SHALL declare appropriate content type produces metadata

#### Scenario: Downloads endpoints declare all error codes
- **WHEN** a Downloads endpoint that returns 400, 404, or 500 is inspected
- **THEN** it SHALL declare `.ProducesProblem()` for each error status code it can return, in addition to 504
