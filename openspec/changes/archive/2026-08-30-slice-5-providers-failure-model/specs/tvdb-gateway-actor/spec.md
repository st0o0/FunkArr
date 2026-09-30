## MODIFIED Requirements

### Requirement: TvdbGatewayActor handles QueryFailure
The `TvdbGatewayActor` SHALL handle `QueryFailure` results from `TvdbProvider` by serving stale cache when available, or propagating the failure to the caller when no cache exists. The actor SHALL log the failure reason and detail.

#### Scenario: Provider failure with stale cache
- **WHEN** `TvdbProvider` returns `QueryFailure(Transport, ...)` and stale cached data exists for the requested show
- **THEN** the gateway SHALL serve the stale cached data and log a warning with the failure reason

#### Scenario: Provider failure without cache
- **WHEN** `TvdbProvider` returns `QueryFailure(NotFound, ...)` and no cached data exists
- **THEN** the gateway SHALL propagate the failure to the caller (empty/null response)

#### Scenario: Rate limited
- **WHEN** `TvdbProvider` returns `QueryFailure(RateLimited, ...)`
- **THEN** the gateway SHALL serve stale cache if available and log a warning

### Requirement: TvdbGatewayActor uses TvdbProvider
The `TvdbGatewayActor` SHALL depend on `TvdbProvider` (renamed from `TvdbV4Client`) and SHALL be located in `Providers/Tvdb/`.

#### Scenario: Provider co-location
- **WHEN** inspecting the `Providers/Tvdb/` folder
- **THEN** it SHALL contain both `TvdbProvider.cs` and `TvdbGatewayActor.cs`
