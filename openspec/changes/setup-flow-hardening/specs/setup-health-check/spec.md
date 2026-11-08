## Purpose

Delta spec for setup health check — adds TVDB/TMDB API key checks and exposes BaseUrl in connectionInfo.

## ADDED Requirements

### Requirement: TVDB API key health check
The setup health check SHALL include a `tvdbApiKey` check that verifies the TVDB API key is configured (non-empty). An empty key SHALL result in `"status": "warn"` with a hint message. A configured key SHALL result in `"status": "ok"`.

#### Scenario: TVDB API key configured
- **WHEN** `TvdbOptions.ApiKey` is set to a non-empty value
- **THEN** the `tvdbApiKey` check SHALL have `"status": "ok"`

#### Scenario: TVDB API key not configured
- **WHEN** `TvdbOptions.ApiKey` is empty or not set
- **THEN** the `tvdbApiKey` check SHALL have `"status": "warn"` and `"message"` containing "Set FunkArr__Tvdb__ApiKey for TV series metadata enrichment"

### Requirement: TMDB API key health check
The setup health check SHALL include a `tmdbApiKey` check that verifies the TMDB API key is configured (non-empty). An empty key SHALL result in `"status": "warn"` with a hint message. A configured key SHALL result in `"status": "ok"`.

#### Scenario: TMDB API key configured
- **WHEN** `TmdbOptions.ApiKey` is set to a non-empty value
- **THEN** the `tmdbApiKey` check SHALL have `"status": "ok"`

#### Scenario: TMDB API key not configured
- **WHEN** `TmdbOptions.ApiKey` is empty or not set
- **THEN** the `tmdbApiKey` check SHALL have `"status": "warn"` and `"message"` containing "Set FunkArr__Tmdb__ApiKey for movie metadata enrichment"

## MODIFIED Requirements

### Requirement: Health check response structure
The response SHALL be a JSON object with a top-level `checks` object containing named check results, and a `connectionInfo` object with the values needed by the setup guide. Each check SHALL have `status` (`"ok"`, `"warn"`, or `"fail"`), an optional `message`, and check-specific fields. The `connectionInfo` object SHALL include `baseUrl` with the value of `FunkArrOptions.BaseUrl` (empty string if not configured).

#### Scenario: Response includes connection info with baseUrl
- **WHEN** `GET /api/system/setup` is requested and `FunkArrOptions.BaseUrl` is `http://funkarr:6969`
- **THEN** the response `connectionInfo` SHALL include `"baseUrl": "http://funkarr:6969"` alongside existing fields

#### Scenario: Response includes empty baseUrl when not configured
- **WHEN** `GET /api/system/setup` is requested and `FunkArrOptions.BaseUrl` is empty
- **THEN** the response `connectionInfo` SHALL include `"baseUrl": ""`

#### Scenario: Response shape
- **WHEN** `GET /api/system/setup` is requested
- **THEN** the response matches the structure `{ "checks": { ..., "tvdbApiKey": { "status": "..." }, "tmdbApiKey": { "status": "..." } }, "connectionInfo": { "baseUrl": "...", "indexerApiPath": "/index/api", "downloadApiPath": "/download/api", "defaultPort": 5000 } }`
