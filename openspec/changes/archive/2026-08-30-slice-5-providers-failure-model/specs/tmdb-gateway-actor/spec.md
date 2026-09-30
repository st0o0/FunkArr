## MODIFIED Requirements

### Requirement: TmdbGatewayActor handles QueryFailure
The `TmdbGatewayActor` SHALL handle `QueryFailure` results from `TmdbProvider` by serving stale cache when available, or propagating the failure to the caller when no cache exists.

#### Scenario: Provider failure with stale cache
- **WHEN** `TmdbProvider` returns `QueryFailure(Transport, ...)` and stale cached data exists
- **THEN** the gateway SHALL serve the stale cached data and log a warning

#### Scenario: Provider failure without cache
- **WHEN** `TmdbProvider` returns `QueryFailure(NotFound, ...)` and no cached data exists
- **THEN** the gateway SHALL propagate the failure (null response)

### Requirement: TmdbGatewayActor uses TmdbProvider
The `TmdbGatewayActor` SHALL depend on `TmdbProvider` (renamed from `TmdbClient`) and SHALL be located in `Providers/Tmdb/`.

#### Scenario: Provider co-location
- **WHEN** inspecting the `Providers/Tmdb/` folder
- **THEN** it SHALL contain both `TmdbProvider.cs` and `TmdbGatewayActor.cs`

## RENAMED Requirements

### Requirement: TmdbClient renamed to TmdbProvider
- **FROM:** `TmdbClient` in `Search/Resolvers/`
- **TO:** `TmdbProvider` in `Providers/Tmdb/`
