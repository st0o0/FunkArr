## ADDED Requirements

### Requirement: MediathekSearchRequest typed request record

FunkArr.Api.Models SHALL define a `MediathekSearchRequest` record used with `[AsParameters]` for the `/api/mediathek/search` endpoint. It SHALL contain: Q (string?), Channel (string?), Topic (string?), DurationMin (int?), DurationMax (int?), Offset (int?), Limit (int?), SortBy (string?), SortOrder (string?). All properties SHALL use `[FromQuery]` attributes.

#### Scenario: Query parameters bind to request record

- **WHEN** a GET request arrives at `/api/mediathek/search?q=tatort&channel=ARD&limit=50`
- **THEN** the `MediathekSearchRequest` SHALL have Q="tatort", Channel="ARD", Limit=50, and all other properties null

#### Scenario: OpenAPI shows query parameters

- **WHEN** the OpenAPI spec is generated for the search endpoint
- **THEN** all 9 query parameters SHALL appear with their types

#### Scenario: Validation logic stays in handler

- **WHEN** all three of Q, Channel, and Topic are null or whitespace
- **THEN** the handler SHALL return 400 Bad Request (validation remains in handler code, not on the model)

### Requirement: DownloadHistoryRequest typed request record

FunkArr.Api.Models SHALL define a `DownloadHistoryRequest` record used with `[AsParameters]` for the `/api/downloads/history` endpoint. It SHALL contain: Start (int?), Limit (int?), Category (string?). All properties SHALL use `[FromQuery]` attributes.

#### Scenario: Query parameters bind to request record

- **WHEN** a GET request arrives at `/api/downloads/history?start=10&limit=25&category=sonarr`
- **THEN** the `DownloadHistoryRequest` SHALL have Start=10, Limit=25, Category="sonarr"

#### Scenario: Default values applied in handler

- **WHEN** Start and Limit are null
- **THEN** the handler SHALL apply defaults (Start=0, Limit=25) — defaulting stays in handler code

### Requirement: RuleSetApiEndpoints list handler extracted

The RuleSetApiEndpoints list endpoint logic SHALL be extracted into a `private static` method (e.g., `HandleList`) matching the pattern of existing `HandleCreate`, `HandleUpdate`, `HandleDelete`, and `HandleExport` methods.

#### Scenario: List handler is a named method

- **WHEN** reviewing RuleSetApiEndpoints code
- **THEN** the list endpoint SHALL delegate to a `private static` method, not contain inline fan-out logic in the lambda

### Requirement: Single SerializeForDisk method

RuleSetApiEndpoints SHALL have exactly one `SerializeForDisk` helper method, not duplicated implementations.

#### Scenario: No duplicate serialization methods

- **WHEN** reviewing RuleSetApiEndpoints code
- **THEN** there SHALL be exactly one `SerializeForDisk` method covering both Create and Update serialization needs
