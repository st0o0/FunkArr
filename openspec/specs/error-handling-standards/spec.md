# error-handling-standards

## Purpose

Defines error handling standards for API endpoints, actors, and PipeTo patterns to ensure exceptions are never silently swallowed and are always logged with structured context.

## Requirements

### Requirement: API endpoint catch blocks SHALL log exceptions

Every `catch (Exception)` block in API endpoint code SHALL log the exception with structured context before returning an error response. The log SHALL use `LogError` level and include domain-relevant identifiers as structured properties.

#### Scenario: API endpoint catches timeout exception

- **WHEN** an API endpoint catches an `Exception` during an actor Ask operation
- **THEN** it SHALL call `logger.LogError(ex, "...")` with a structured template including the operation name and relevant entity ID before returning the error response

#### Scenario: No silent exception swallowing in endpoints

- **WHEN** any `catch (Exception)` block exists in FunkArr.Api or FunkArr.ArrApi endpoint code
- **THEN** the caught exception SHALL be captured in a variable and passed to a logging call

### Requirement: API endpoints SHALL receive ILogger via DI

All API endpoint static methods that contain `try/catch` blocks SHALL accept `ILogger<T>` as a parameter resolved via `[FromServices]`, where `T` is the endpoint class.

#### Scenario: Endpoint method receives logger

- **WHEN** a static endpoint method in `MediathekApiEndpoints`, `DownloadsApiEndpoints`, `RuleSetApiEndpoints`, `SystemApiEndpoints`, `SearchHandler`, or `SabnzbdApiEndpoints` contains a `try/catch` block
- **THEN** it SHALL have an `ILogger` parameter for logging within the catch block

### Requirement: PipeTo failure handlers SHALL forward exceptions

Every `PipeTo` call with a `failure:` handler SHALL forward the exception into the typed failure message. Failure handlers SHALL NOT discard the exception with `_ =>` or ignore the exception parameter.

#### Scenario: PipeTo failure wraps exception

- **WHEN** a `PipeTo` call has a `failure:` lambda
- **THEN** the lambda SHALL capture the exception and include it in the failure message (e.g., `failure: ex => new SomeFailed(ex)`)

#### Scenario: No discarded exceptions in PipeTo

- **WHEN** `MovieSearchWorker` or `TvSearchWorker` PipeTo handlers handle a `RuleSetNotFound` failure path
- **THEN** the exception SHALL be forwarded into the failure message, not discarded

### Requirement: Bare catch blocks SHALL capture the exception variable

No `catch (Exception)` block (without a variable) SHALL exist in production code. All catch blocks SHALL capture the exception in a named variable for logging.

#### Scenario: RuleSetManagerState summary catch

- **WHEN** `RuleSetManagerState` catches an exception during summary building
- **THEN** the exception SHALL be captured in a variable and logged at Warning level

### Requirement: Actor logging on key transitions

Actors SHALL log at appropriate levels for key state transitions and failures. Debug level for message receipt and normal transitions, Warning for transient failures, Error for unrecoverable failures.

#### Scenario: Actor logs message receipt at Debug

- **WHEN** an actor receives a command or query message
- **THEN** it MAY log the receipt at Debug level with the message type and relevant entity ID

#### Scenario: Actor logs failure at Warning

- **WHEN** an actor handles a failure message (e.g., `MediathekQueryFailed`, `ScoringFailed`)
- **THEN** it SHALL log at Warning level including the reason and cause exception if available
