## RENAMED Requirements

### Requirement: Setup health check endpoint
- **FROM:** `GET /api/health/setup`
- **TO:** `GET /api/system/setup`

### Requirement: Storage status endpoint
- **FROM:** `GET /api/health/storage`
- **TO:** `GET /api/system/storage`

### Requirement: Cache stats endpoint
- **FROM:** `GET /api/health/cache`
- **TO:** `GET /api/system/cache`

## MODIFIED Requirements

### Requirement: Setup health check endpoint
The system SHALL expose `GET /api/system/setup` that returns a JSON object with the results of all operational readiness checks. The endpoint SHALL have no authentication. All checks SHALL run concurrently via `Task.WhenAll` to minimize response time. The endpoint SHALL be registered under the OpenAPI tag `"System"`.

#### Scenario: All checks pass
- **WHEN** `GET /api/system/setup` is requested and all prerequisites are met
- **THEN** the response status is 200 and every check entry has `"status": "ok"`

#### Scenario: Endpoint responds without authentication
- **WHEN** `GET /api/system/setup` is requested without an API key
- **THEN** the response status is 200 (not 401 or 403)

### Requirement: Storage status endpoint
The system SHALL expose `GET /api/system/storage` that returns storage directory information. The endpoint SHALL be registered under the OpenAPI tag `"System"`.

#### Scenario: Storage status response
- **WHEN** `GET /api/system/storage` is requested
- **THEN** the response is 200 with JSON containing complete and incomplete directory info

### Requirement: Cache stats endpoint
The system SHALL expose `GET /api/system/cache` that returns metadata cache statistics. The endpoint SHALL be registered under the OpenAPI tag `"System"`.

#### Scenario: Cache stats response
- **WHEN** `GET /api/system/cache` is requested
- **THEN** the response is 200 with JSON containing TVDB entries, TMDB entries, and oldest entry timestamp
