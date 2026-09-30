## ADDED Requirements

### Requirement: All HttpClients SHALL have a standard resilience pipeline

Every HttpClient registered via `IHttpClientFactory` SHALL have a standard resilience pipeline configured via `AddStandardResilienceHandler()` from `Microsoft.Extensions.Http.Resilience`. The pipeline SHALL include retry, circuit breaker, and timeout layers.

#### Scenario: MediathekViewWeb client has resilience

- **WHEN** the "MediathekViewWeb" named HttpClient is registered in `MediathekSetupContainer`
- **THEN** it SHALL have `AddStandardResilienceHandler()` chained to the registration

#### Scenario: GitHub client has resilience

- **WHEN** the "GitHub" named HttpClient is registered in `RuleSetSetupContainer`
- **THEN** it SHALL have `AddStandardResilienceHandler()` chained to the registration

#### Scenario: TvdbClient has resilience

- **WHEN** the `TvdbClient` typed HttpClient is registered in `MetadataSetupContainer`
- **THEN** it SHALL have `AddStandardResilienceHandler()` chained to the registration

#### Scenario: TmdbClient has resilience

- **WHEN** the `TmdbClient` typed HttpClient is registered in `MetadataSetupContainer`
- **THEN** it SHALL have `AddStandardResilienceHandler()` chained to the registration

#### Scenario: SubtitlePreparer client has resilience

- **WHEN** the `SubtitlePreparer` typed HttpClient is registered in `DownloadServiceExtensions`
- **THEN** it SHALL have `AddStandardResilienceHandler()` chained to the registration

### Requirement: Per-client timeout overrides

Each HttpClient MAY override the default timeout values based on its usage pattern. The standard defaults (30s total, 10s per attempt, 3 retries) SHALL apply unless explicitly overridden.

#### Scenario: MediathekViewWeb uses longer timeouts

- **WHEN** the MediathekViewWeb client is configured
- **THEN** it SHALL have a total request timeout of 45 seconds and an attempt timeout of 15 seconds to accommodate slow search queries

#### Scenario: SubtitlePreparer uses shorter timeouts

- **WHEN** the SubtitlePreparer client is configured
- **THEN** it SHALL have a total request timeout of 15 seconds and an attempt timeout of 5 seconds because subtitle fetching is optional and should fail fast

#### Scenario: Default timeouts for metadata clients

- **WHEN** TvdbClient and TmdbClient are configured
- **THEN** they SHALL use the standard default timeouts (30s total, 10s per attempt)

#### Scenario: Default timeouts for GitHub client

- **WHEN** the GitHub client is configured
- **THEN** it SHALL use the standard default timeouts (30s total, 10s per attempt)

### Requirement: Resilience pipeline SHALL NOT interfere with application-level retry logic

The resilience pipeline handles transient HTTP failures (5xx, 408, 429). Application-level retry logic (e.g., TvdbClient 401 reauth) SHALL remain in place and operate independently.

#### Scenario: TvdbClient 401 reauth is unaffected

- **WHEN** the TVDB API returns 401 Unauthorized
- **THEN** the resilience pipeline SHALL NOT retry the 401 (it is not a transient error)
- **AND** the TvdbClient's existing reauth logic SHALL handle the 401 and retry the request

#### Scenario: Transient error after reauth

- **WHEN** the TvdbClient re-authenticates and the subsequent request fails with a 503
- **THEN** the resilience pipeline SHALL retry the request according to its retry policy

### Requirement: Microsoft.Extensions.Http.Resilience package via central package management

The `Microsoft.Extensions.Http.Resilience` package SHALL be added to `Directory.Packages.props` and referenced in projects that register HttpClients.

#### Scenario: Package in central management

- **WHEN** the solution is built
- **THEN** `Directory.Packages.props` SHALL contain a `PackageVersion` entry for `Microsoft.Extensions.Http.Resilience`

#### Scenario: Package referenced in host project

- **WHEN** the `FunkArr` host project is compiled
- **THEN** it SHALL have a `PackageReference` to `Microsoft.Extensions.Http.Resilience`

#### Scenario: Package referenced in download project

- **WHEN** the `FunkArr.Download` project is compiled
- **THEN** it SHALL have a `PackageReference` to `Microsoft.Extensions.Http.Resilience`
