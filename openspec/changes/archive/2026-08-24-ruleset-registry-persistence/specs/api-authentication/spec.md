## ADDED Requirements

### Requirement: API key middleware
The system SHALL register an `ApiKeyMiddleware` in the HTTP pipeline before `MapControllers()` that validates incoming requests against the configured `FunkArr:ApiKey`.

#### Scenario: Valid API key in query string
- **WHEN** a request to `/api/v1/rulesets?apikey=correct-key` is received
- **THEN** the middleware SHALL pass the request through to the controller

#### Scenario: Missing API key
- **WHEN** a request to `/api/v1/rulesets` is received without an `apikey` parameter
- **THEN** the middleware SHALL return HTTP 401 with a JSON error body

#### Scenario: Invalid API key
- **WHEN** a request to `/api/v1/rulesets?apikey=wrong-key` is received
- **THEN** the middleware SHALL return HTTP 401 with a JSON error body

### Requirement: Protected path prefixes
The middleware SHALL protect requests whose path starts with `/api/`, `/index/` (Newznab), or `/download/` (SABnzbd).

#### Scenario: Non-API paths are not protected
- **WHEN** a request to `/alive` or `/metrics` or a static file is received
- **THEN** the middleware SHALL NOT check for an API key

#### Scenario: Newznab paths are protected
- **WHEN** a request to `/index/api?t=search&q=Tatort` is received without an apikey
- **THEN** the middleware SHALL return HTTP 401

### Requirement: Exempt endpoints
The middleware SHALL exempt the following endpoints from API key validation: Newznab caps (`/index/api?t=caps`), setup status (`/api/v1/setup/status`).

#### Scenario: Caps endpoint without API key
- **WHEN** `GET /index/api?t=caps` is received without an apikey parameter
- **THEN** the middleware SHALL pass the request through (Newznab convention: caps is public)

#### Scenario: Setup status without API key
- **WHEN** `GET /api/v1/setup/status` is received without an apikey
- **THEN** the middleware SHALL pass the request through (needed before auth is configured)

### Requirement: API key source
The middleware SHALL read the API key from the `apikey` query string parameter. This matches the Newznab and SABnzbd convention.

#### Scenario: API key in query string
- **WHEN** a request includes `?apikey=my-key`
- **THEN** the middleware SHALL compare `my-key` against `FunkArrOptions.ApiKey`
