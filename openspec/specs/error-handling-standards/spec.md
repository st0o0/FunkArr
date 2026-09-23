# error-handling-standards

## Purpose

Defines error handling standards for API endpoints, actors, and PipeTo patterns to ensure exceptions are never silently swallowed and are always logged with structured context.

## Requirements

### Requirement: API endpoint catch blocks SHALL log exceptions

API endpoint methods that contain try/catch blocks SHALL log the caught exception using `ILogger`. TimeoutException SHALL be logged at Warning level and produce a 504 response. Other exceptions SHALL be logged at Error level and produce a 500 response with a generic message. The `EndpointExceptionFilter` SHALL NOT include `ex.Message` in the response body — it SHALL use a fixed generic message ("An unexpected error occurred") and log the detail server-side only.

#### Scenario: Timeout exception in endpoint

- **WHEN** an endpoint catches a `TimeoutException`
- **THEN** the exception is logged at Warning level
- **THEN** a 504 Gateway Timeout response is returned

#### Scenario: General exception in endpoint

- **WHEN** an endpoint catches a non-timeout exception
- **THEN** the exception is logged at Error level
- **THEN** a 500 response is returned with title "Internal Server Error" and no exception detail in the body

#### Scenario: Exception message not leaked

- **WHEN** the `EndpointExceptionFilter` catches any exception
- **THEN** the response body contains "An unexpected error occurred" as the detail, not the exception message

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
