## ADDED Requirements

### Requirement: API key status endpoint
The system SHALL provide `GET /api/v1/setup/api-keys` returning the configuration and validity status of TVDB and TMDB API keys.

#### Scenario: Both keys configured and valid
- **WHEN** both `TvdbApiKey` and `TmdbApiKey` are set and test calls succeed
- **THEN** the endpoint SHALL return `{ tvdb: { configured: true, valid: true }, tmdb: { configured: true, valid: true } }`

#### Scenario: Keys not configured
- **WHEN** neither key is set
- **THEN** the endpoint SHALL return `{ tvdb: { configured: false, valid: false }, tmdb: { configured: false, valid: false } }`

#### Scenario: Key configured but invalid
- **WHEN** a key is set but the test call fails (wrong key)
- **THEN** the endpoint SHALL return `configured: true, valid: false` for that key

### Requirement: API key validation on startup
The system SHALL test API key validity at startup by making lightweight calls (TVDB: series lookup, TMDB: find by IMDB ID) and cache the results. Invalid keys SHALL produce a log warning but SHALL NOT prevent startup.

#### Scenario: Valid keys at startup
- **WHEN** the application starts with valid TVDB and TMDB keys
- **THEN** the system SHALL log an info message confirming key validity

#### Scenario: Invalid key at startup
- **WHEN** the application starts with an invalid TVDB key
- **THEN** the system SHALL log a warning "TVDB API key is configured but invalid" and continue startup

#### Scenario: Missing keys at startup
- **WHEN** the application starts without API keys
- **THEN** the system SHALL log an info message "TVDB/TMDB keys not configured — auto-generation disabled" and continue startup
