## ADDED Requirements

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

### Requirement: Package dependency
The system SHALL add `Scalar.AspNetCore` to `Directory.Packages.props` and reference it in the host project (`FunkArr.csproj`).

#### Scenario: Package is declared centrally
- **WHEN** the solution is built
- **THEN** `Scalar.AspNetCore` is resolved from `Directory.Packages.props`
