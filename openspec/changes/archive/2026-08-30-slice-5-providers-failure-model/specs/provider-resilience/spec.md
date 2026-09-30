## ADDED Requirements

### Requirement: Standard resilience on all HTTP clients
The system SHALL add `AddStandardResilienceHandler()` from `Microsoft.Extensions.Http.Resilience` to all typed HTTP client registrations. This provides retry with exponential backoff, circuit breaker, and timeout as a single composable handler.

#### Scenario: MediathekProvider HTTP client
- **WHEN** `MediathekProvider` (formerly `MediathekClient`) is registered via `AddHttpClient`
- **THEN** the registration SHALL include `.AddStandardResilienceHandler()`

#### Scenario: TvdbProvider HTTP client
- **WHEN** `TvdbProvider` (formerly `TvdbV4Client`) is registered via `AddHttpClient`
- **THEN** the registration SHALL include `.AddStandardResilienceHandler()`

#### Scenario: TmdbProvider HTTP client
- **WHEN** `TmdbProvider` (formerly `TmdbClient`) is registered via `AddHttpClient`
- **THEN** the registration SHALL include `.AddStandardResilienceHandler()`

#### Scenario: GitHubProvider HTTP client
- **WHEN** `GitHubProvider` (formerly `GitHubReleaseClient`) is registered via `AddHttpClient`
- **THEN** the registration SHALL include `.AddStandardResilienceHandler()`

### Requirement: Microsoft.Extensions.Http.Resilience package
The system SHALL add `Microsoft.Extensions.Http.Resilience` to `Directory.Packages.props` as a centrally managed package.

#### Scenario: Package present
- **WHEN** inspecting `Directory.Packages.props`
- **THEN** `Microsoft.Extensions.Http.Resilience` SHALL be listed with a version
