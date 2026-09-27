# integration-test-system Specification

## Purpose

Contract tests for the System, Setup, and Mediathek API endpoints, verifying health checks, configuration, and search with typed response deserialization.

## Requirements

### Requirement: System API endpoints have contract tests
Every System API endpoint SHALL have at least one integration test with typed response deserialization.

#### Scenario: Version returns app and ruleset version
- **WHEN** `GET /api/system/version` is requested
- **THEN** the typed `VersionResponse` SHALL contain a non-null application version

#### Scenario: Storage returns directory info
- **WHEN** `GET /api/system/storage` is requested
- **THEN** the typed `StorageStatusResponse` SHALL contain complete and incomplete directory paths

#### Scenario: Cache stats returns counts
- **WHEN** `GET /api/system/cache` is requested and the probe responds
- **THEN** the typed `CacheStatsResponse` SHALL contain TVDB and TMDB entry counts

### Requirement: Setup API endpoints have contract tests
Every Setup API endpoint SHALL have at least one integration test verifying the HTTP response shape.

#### Scenario: Setup health check returns all checks
- **WHEN** `GET /api/system/setup` is requested
- **THEN** the typed `SetupHealthCheck` SHALL contain entries for apiKey, mediathekViewWeb, dataDirectory, and ffmpeg

### Requirement: Mediathek search endpoint has contract tests
The Mediathek search endpoint SHALL have integration tests verifying request validation and response shape.

#### Scenario: Search with results
- **WHEN** `GET /api/mediathek/search?q=tatort` is requested and the probe responds with items
- **THEN** the typed response SHALL contain search results with channel, topic, title, and URL fields

#### Scenario: Search without parameters returns 400
- **WHEN** `GET /api/mediathek/search` is requested without q, channel, or topic
- **THEN** the response SHALL be 400
