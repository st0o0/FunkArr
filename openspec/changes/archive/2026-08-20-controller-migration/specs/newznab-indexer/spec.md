## MODIFIED Requirements

### Requirement: API key validation
The system SHALL validate the `apikey` query parameter on all Newznab endpoints. Requests with missing or invalid API keys MUST receive a Newznab error response. Authentication SHALL be handled by the centralized `ApiKeyMiddleware` instead of a dedicated `ApiKeyFilter`.

#### Scenario: Missing API key
- **WHEN** a client sends `GET /api?t=tvsearch&tvdbid=12345` without an `apikey` parameter
- **THEN** the `ApiKeyMiddleware` SHALL return HTTP 200 with Newznab error XML (code 100, "Incorrect user credentials")

#### Scenario: Valid API key
- **WHEN** a client sends a request with a valid `apikey`
- **THEN** the request is processed normally

## ADDED Requirements

### Requirement: Controller-based implementation
The Newznab endpoints SHALL be implemented as an MVC controller (`NewznabController`) in the `FunkArr.Api` namespace, located in `src/FunkArr/Api/`. The controller SHALL be marked as version-neutral (no URL version segment).

#### Scenario: Newznab route unchanged
- **WHEN** a client sends `GET /api?t=caps&apikey=key`
- **THEN** the system SHALL route to `NewznabController` at the same `/api` path as before

### Requirement: OpenAPI tagging
The Newznab controller SHALL be tagged with `"Newznab Emulation"` for API documentation grouping.

#### Scenario: Scalar documentation grouping
- **WHEN** the OpenAPI spec is rendered in Scalar
- **THEN** Newznab endpoints SHALL appear under the "Newznab Emulation" group
