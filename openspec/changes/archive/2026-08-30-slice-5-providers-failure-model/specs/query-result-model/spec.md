## ADDED Requirements

### Requirement: IQueryResult interface hierarchy
The system SHALL provide an open interface hierarchy in `FunkArr.Core/Results/` for typed provider results:
- `IQueryResult` — marker interface
- `QuerySuccess<T>(T Value) : IQueryResult` — successful result carrying the typed value
- `QueryFailure(FailureReason Reason, string? Detail) : IQueryResult` — failure result with reason and optional detail

#### Scenario: Success result
- **WHEN** a provider successfully fetches data
- **THEN** it SHALL return `QuerySuccess<T>` with the result value

#### Scenario: Failure result
- **WHEN** a provider encounters an error
- **THEN** it SHALL return `QueryFailure` with the appropriate `FailureReason` and a detail string

#### Scenario: Open hierarchy consumer
- **WHEN** a consumer switches on `IQueryResult`
- **THEN** it SHALL include a `default` arm since the hierarchy is open and the compiler cannot prove exhaustiveness

### Requirement: FailureReason enum
The system SHALL provide a `FailureReason` enum in `FunkArr.Core/Results/` with values: `NotFound`, `Unauthorized`, `RateLimited`, `Timeout`, `Cancelled`, `Transport`, `Malformed`.

#### Scenario: HTTP 404 maps to NotFound
- **WHEN** a provider receives HTTP 404
- **THEN** it SHALL return `QueryFailure(FailureReason.NotFound, ...)`

#### Scenario: HTTP 401/403 maps to Unauthorized
- **WHEN** a provider receives HTTP 401 or 403
- **THEN** it SHALL return `QueryFailure(FailureReason.Unauthorized, ...)`

#### Scenario: HTTP 429 maps to RateLimited
- **WHEN** a provider receives HTTP 429
- **THEN** it SHALL return `QueryFailure(FailureReason.RateLimited, ...)`

#### Scenario: TaskCanceledException maps to Timeout or Cancelled
- **WHEN** a provider catches a `TaskCanceledException`
- **THEN** it SHALL return `Timeout` if the token was not cancelled, `Cancelled` if it was

#### Scenario: HttpRequestException maps to Transport
- **WHEN** a provider catches an `HttpRequestException`
- **THEN** it SHALL return `QueryFailure(FailureReason.Transport, ex.Message)`

#### Scenario: Deserialization failure maps to Malformed
- **WHEN** a provider receives a response that cannot be deserialized
- **THEN** it SHALL return `QueryFailure(FailureReason.Malformed, ...)`

### Requirement: Provider boundary contract
Every provider SHALL return `IQueryResult` (or a typed `Task<IQueryResult>`) from its public methods. No provider SHALL throw past its own boundary. Each provider SHALL have exactly one catch site that translates exceptions to `QueryFailure`.

#### Scenario: No exceptions cross provider boundary
- **WHEN** any exception occurs inside a provider method
- **THEN** it SHALL be caught and translated to `QueryFailure` — the caller SHALL never see the exception

#### Scenario: Gateway decides failure semantics
- **WHEN** a gateway actor receives `QueryFailure` from its provider
- **THEN** the gateway SHALL decide the response: serve stale cache, degrade, or propagate the failure
