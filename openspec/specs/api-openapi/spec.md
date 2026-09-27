## Purpose

OpenAPI document generation and interactive API reference UI for the internal API.

## Requirements

### Requirement: OpenAPI document endpoint
The system SHALL expose an OpenAPI 3.x document at `/openapi/v1.json` generated from the registered endpoints via `AddOpenApi()` and `MapOpenApi()`.

#### Scenario: OpenAPI document is accessible
- **WHEN** `GET /openapi/v1.json` is called
- **THEN** the response is 200 with content type `application/json` containing a valid OpenAPI document

### Requirement: Scalar API reference UI
The system SHALL serve the Scalar API reference UI at `/scalar` using `Scalar.AspNetCore`. The UI SHALL load the OpenAPI document and provide interactive API exploration.

#### Scenario: Scalar UI is accessible
- **WHEN** `GET /scalar` is called in a browser
- **THEN** the page renders the Scalar API reference interface

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

### Requirement: Package dependency
The system SHALL add `Scalar.AspNetCore` to `Directory.Packages.props` and reference it in the host project (`FunkArr.csproj`).

#### Scenario: Package is declared centrally
- **WHEN** the solution is built
- **THEN** `Scalar.AspNetCore` is resolved from `Directory.Packages.props`
