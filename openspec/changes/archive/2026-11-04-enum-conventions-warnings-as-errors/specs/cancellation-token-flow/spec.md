## ADDED Requirements

### Requirement: Tests pass the test cancellation token
Test code that calls a method accepting a `CancellationToken` SHALL pass `TestContext.Current.CancellationToken`, so running tests respond to cancellation.

#### Scenario: Async call in a test
- **WHEN** a test calls an async method that has a `CancellationToken` parameter
- **THEN** it passes `TestContext.Current.CancellationToken`

#### Scenario: xUnit1051 not suppressed
- **WHEN** a test omits the token
- **THEN** analyzer xUnit1051 reports it and, under warnings-as-errors, the build fails
