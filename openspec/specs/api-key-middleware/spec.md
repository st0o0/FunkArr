## Purpose

Centralized API key authentication middleware replacing per-endpoint auth filters. Route-aware error response format (XML for Newznab, JSON for everything else).

## Requirements

### Requirement: Centralized API key authentication
The system SHALL authenticate API requests via a single `ApiKeyMiddleware` in the ASP.NET request pipeline. The middleware SHALL read the API key from the `apikey` query parameter and compare it against the configured `FunkArrOptions.ApiKey`.

#### Scenario: Valid API key on versioned endpoint
- **WHEN** a request to `/api/v1/queue?apikey=valid-key` is received
- **THEN** the middleware SHALL pass the request through to the controller

#### Scenario: Missing API key on versioned endpoint
- **WHEN** a request to `/api/v1/queue` is received without an `apikey` parameter
- **THEN** the middleware SHALL return HTTP 401 with JSON body `{ "error": "Incorrect user credentials" }`

#### Scenario: Invalid API key on versioned endpoint
- **WHEN** a request to `/api/v1/queue?apikey=wrong-key` is received
- **THEN** the middleware SHALL return HTTP 401 with JSON body `{ "error": "Incorrect user credentials" }`

### Requirement: Route-aware error response format
The middleware SHALL return authentication errors in the format expected by the calling client, determined by the request path.

#### Scenario: Newznab XML error format
- **WHEN** authentication fails for a request to the bare `/api` path (Newznab endpoint)
- **THEN** the middleware SHALL return `application/xml` content with Newznab error code 100 ("Incorrect user credentials")

#### Scenario: JSON error format for all other protected routes
- **WHEN** authentication fails for any request under `/api/v1/*`, `/download/api`, or other protected paths
- **THEN** the middleware SHALL return HTTP 401 with `application/json` content `{ "error": "Incorrect user credentials" }`

### Requirement: Authentication bypass for public routes
The middleware SHALL skip authentication for routes that do not require an API key.

#### Scenario: Health check endpoints bypass auth
- **WHEN** a request to `/healthz` or `/alive` is received without an API key
- **THEN** the middleware SHALL pass the request through without checking authentication

#### Scenario: Fake NZB download bypasses auth
- **WHEN** a request to `/api/fake_nzb` is received without an API key
- **THEN** the middleware SHALL pass the request through (NZB download URLs are shared with Sonarr/Radarr without API key)

#### Scenario: Static files and documentation bypass auth
- **WHEN** a request to a static file, `/scalar`, or `/openapi` path is received
- **THEN** the middleware SHALL pass the request through without checking authentication
