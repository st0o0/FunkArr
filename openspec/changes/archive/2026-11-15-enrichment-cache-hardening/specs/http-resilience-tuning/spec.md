## MODIFIED Requirements

### Requirement: External API HttpClients SHALL have profile-tuned circuit breakers

Each external API HttpClient registration SHALL configure the `StandardResilienceHandler` circuit breaker with break duration and failure ratio threshold appropriate to the API's characteristics. The total request timeout SHALL be aligned with the caller's Ask timeout to prevent late replies.

- TVDB and TMDB: `BreakDuration = 30s`, `FailureRatioThreshold = 0.25`, `TotalRequestTimeout = 8s`
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

#### Scenario: TVDB total timeout aligned with enrichment Ask
- **WHEN** a TVDB HTTP request is in flight
- **THEN** the `StandardResilienceHandler` total request timeout SHALL be 8 seconds, ensuring the request completes or cancels before the EvaluationWorker's 10-second Ask timeout expires

#### Scenario: TMDB total timeout aligned with enrichment Ask
- **WHEN** a TMDB HTTP request is in flight
- **THEN** the `StandardResilienceHandler` total request timeout SHALL be 8 seconds
