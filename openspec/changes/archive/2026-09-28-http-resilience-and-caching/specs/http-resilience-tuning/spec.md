## ADDED Requirements

### Requirement: External API HttpClients SHALL have profile-tuned circuit breakers

Each external API HttpClient registration SHALL configure the `StandardResilienceHandler` circuit breaker with break duration and failure ratio threshold appropriate to the API's characteristics.

- TVDB and TMDB: `BreakDuration = 30s`, `FailureRatioThreshold = 0.25`
- MediathekViewWeb: `BreakDuration = 60s`, `FailureRatioThreshold = 0.20`
- GitHub and route:* clients: unchanged (defaults)

#### Scenario: TVDB circuit breaker trips on sustained failures

- **WHEN** 25% or more of TVDB requests fail within the sampling window
- **THEN** the circuit breaker SHALL open for 30 seconds before allowing a probe request

#### Scenario: TMDB circuit breaker trips on sustained failures

- **WHEN** 25% or more of TMDB requests fail within the sampling window
- **THEN** the circuit breaker SHALL open for 30 seconds before allowing a probe request

#### Scenario: MediathekViewWeb circuit breaker trips on sustained failures

- **WHEN** 20% or more of MediathekViewWeb requests fail within the sampling window
- **THEN** the circuit breaker SHALL open for 60 seconds before allowing a probe request

#### Scenario: GitHub and route clients keep default circuit breaker

- **WHEN** the GitHub or route:* HttpClients are registered
- **THEN** their `StandardResilienceHandler` circuit breaker settings SHALL remain at framework defaults

### Requirement: TVDB and TMDB retry policies SHALL respect Retry-After headers

The `StandardResilienceHandler` retry configuration for TVDB and TMDB clients SHALL use a custom `DelayGenerator` that reads the `Retry-After` response header when the server returns HTTP 429.

#### Scenario: 429 with Retry-After header

- **WHEN** the API returns HTTP 429 with a `Retry-After: 5` header
- **THEN** the retry delay SHALL be 5 seconds (parsed from the header), capped at 60 seconds

#### Scenario: 429 without Retry-After header

- **WHEN** the API returns HTTP 429 without a `Retry-After` header
- **THEN** the retry SHALL fall back to the default exponential backoff delay

#### Scenario: Retry-After value exceeds cap

- **WHEN** the API returns `Retry-After: 120`
- **THEN** the retry delay SHALL be capped at 60 seconds

### Requirement: Retry-After delay generator SHALL be a shared static helper

A `RetryAfterDefaults` static class in `FunkArr/Configuration/` SHALL provide a reusable `DelayGenerator` method that both TVDB and TMDB client registrations reference.

#### Scenario: Both enrichment clients use the same delay generator

- **WHEN** `EnrichmentSetupContainer` registers TVDB and TMDB HttpClients
- **THEN** both SHALL reference `RetryAfterDefaults` for their retry `DelayGenerator`
