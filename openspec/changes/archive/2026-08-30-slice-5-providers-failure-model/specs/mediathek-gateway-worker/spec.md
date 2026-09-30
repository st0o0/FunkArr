## MODIFIED Requirements

### Requirement: MediathekGatewayActor handles QueryFailure
The `MediathekGatewayActor` SHALL handle `QueryFailure` results from `MediathekProvider` by propagating the failure to the caller (no cache for search results). The actor SHALL log the failure reason and detail.

#### Scenario: Provider failure
- **WHEN** `MediathekProvider` returns `QueryFailure(Transport, ...)`
- **THEN** the gateway SHALL propagate the failure to the caller and log a warning

#### Scenario: Timeout
- **WHEN** `MediathekProvider` returns `QueryFailure(Timeout, ...)`
- **THEN** the gateway SHALL propagate the failure and log a warning

### Requirement: MediathekGatewayActor uses MediathekProvider
The `MediathekGatewayActor` SHALL depend on `MediathekProvider` (renamed from `MediathekClient`) and SHALL be located in `Providers/Mediathek/`.

#### Scenario: Provider co-location
- **WHEN** inspecting the `Providers/Mediathek/` folder
- **THEN** it SHALL contain both `MediathekProvider.cs` and `MediathekGatewayActor.cs`

## RENAMED Requirements

### Requirement: MediathekClient renamed to MediathekProvider
- **FROM:** `MediathekClient` in `Search/`
- **TO:** `MediathekProvider` in `Providers/Mediathek/`

## ADDED Requirements

### Requirement: MediathekProvider returns IQueryResult
All public methods on `MediathekProvider` SHALL return `Task<IQueryResult>` instead of throwing.

#### Scenario: Successful query
- **WHEN** MediathekViewWeb returns results
- **THEN** `MediathekProvider` SHALL return `QuerySuccess<MediathekResultItem[]>(items)`

#### Scenario: Network error
- **WHEN** an HttpRequestException occurs
- **THEN** `MediathekProvider` SHALL return `QueryFailure(Transport, ex.Message)`
