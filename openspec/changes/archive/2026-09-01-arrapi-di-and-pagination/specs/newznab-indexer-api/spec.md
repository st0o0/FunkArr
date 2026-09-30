## MODIFIED Requirements

### Requirement: Caps limits reflect actual defaults

The Caps response SHALL declare `<limits max="500" default="100"/>` to reflect the actual pagination behavior.

#### Scenario: Limits declared

- **WHEN** the caps XML is returned
- **THEN** `<limits>` SHALL have `max="500"` and `default="100"`

### Requirement: Search pagination parameters

The system SHALL accept `offset` (int, default 0) and `limit` (int, default 100) query parameters on all search endpoints (`t=search`, `t=tvsearch`, `t=movie`). The system SHALL cap `limit` to the Caps-advertised max (500) before forwarding to the search pipeline.

#### Scenario: Pagination parameters forwarded

- **WHEN** `?t=tvsearch&q=Tatort&offset=10&limit=25` is requested
- **THEN** the system SHALL forward Limit=25 and Offset=10 to the TvSearchCommand

#### Scenario: Default pagination

- **WHEN** a search request omits `offset` and `limit`
- **THEN** the system SHALL forward Limit=null and Offset=null (workers apply their own defaults)

#### Scenario: Limit exceeds max

- **WHEN** `?t=tvsearch&q=Tatort&limit=1000` is requested
- **THEN** the system SHALL cap limit to 500 before forwarding to the search pipeline

#### Scenario: RSS response pagination

- **WHEN** search results are returned with offset=10
- **THEN** the RSS response SHALL have `<newznab:response offset="10" total="N"/>` where N is the total number of matching items

### Requirement: IndexerApiEndpoints resolves dependencies via DI

`MapIndexerApi` SHALL be a parameterless extension method on `WebApplication`. The endpoint handler SHALL resolve `IActorRegistry` and `IOptions<FunkArrOptions>` via Minimal API DI parameter injection instead of receiving them as closure-captured values.

#### Scenario: Endpoint resolves actor registry from DI

- **WHEN** a search request arrives at `/index/api`
- **THEN** the handler SHALL resolve `IActorRegistry` from DI and look up the SearchManager actor

#### Scenario: API key validated from options

- **WHEN** a request arrives at `/index/api`
- **THEN** the `ApiKeyEndpointFilter` SHALL resolve the API key from `IOptions<FunkArrOptions>` via `HttpContext.RequestServices`
