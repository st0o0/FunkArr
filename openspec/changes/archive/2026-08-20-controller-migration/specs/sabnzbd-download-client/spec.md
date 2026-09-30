## MODIFIED Requirements

### Requirement: API key validation
The system SHALL validate the `apikey` query parameter on all SABnzbd endpoints. Authentication SHALL be handled by the centralized `ApiKeyMiddleware` instead of the inline `ValidateApiKey` method.

#### Scenario: Missing API key
- **WHEN** a client sends a request without `apikey`
- **THEN** the `ApiKeyMiddleware` SHALL return JSON with HTTP 401 and an authentication error

## ADDED Requirements

### Requirement: Controller-based implementation
The SABnzbd endpoints SHALL be implemented as an MVC controller (`SabnzbdController`) in the `FunkArr.Api` namespace, located in `src/FunkArr/Api/`. The controller SHALL be marked as version-neutral (no URL version segment).

#### Scenario: SABnzbd route unchanged
- **WHEN** a client sends `GET /download/api?mode=version&apikey=key`
- **THEN** the system SHALL route to `SabnzbdController` at the same `/download/api` path as before

### Requirement: OpenAPI tagging
The SABnzbd controller SHALL be tagged with `"SABnzbd Emulation"` for API documentation grouping.

#### Scenario: Scalar documentation grouping
- **WHEN** the OpenAPI spec is rendered in Scalar
- **THEN** SABnzbd endpoints SHALL appear under the "SABnzbd Emulation" group
