# cancellation-token-flow Specification

## Purpose
TBD - created by archiving change thread-cancellation-token. Update Purpose after archive.
## Requirements
### Requirement: Minimal API endpoints accept CancellationToken
All Minimal API endpoint lambdas in FunkArr.Api SHALL accept `CancellationToken` as a parameter. The framework binds it automatically from `HttpContext.RequestAborted`.

#### Scenario: Endpoint lambda signature includes CancellationToken
- **WHEN** a Minimal API endpoint is defined
- **THEN** its lambda signature includes `CancellationToken ct` as the last parameter

### Requirement: Controller actions accept CancellationToken
All controller action methods in FunkArr.ArrApi SHALL accept `CancellationToken` as the last parameter. The framework binds it automatically from `HttpContext.RequestAborted`.

#### Scenario: Controller action signature includes CancellationToken
- **WHEN** a controller action method is defined
- **THEN** its method signature includes `CancellationToken cancellationToken` as the last parameter

### Requirement: Service methods forward CancellationToken
Public service methods in FunkArr.ArrApi that perform actor Ask calls SHALL accept `CancellationToken` as a parameter and forward it to the Ask call.

#### Scenario: Service method receives and forwards CancellationToken
- **WHEN** a service method calls `Ask<T>` on an actor
- **THEN** the method accepts `CancellationToken` and passes it as the third argument to `Ask<T>(message, timeout, ct)`

### Requirement: API Ask calls use timeout and CancellationToken
All `Ask<T>` calls in the API layer (FunkArr.Api and FunkArr.ArrApi) SHALL use the 3-arg overload `Ask<T>(message, timeout, ct)` combining both the actor timeout and the HTTP request's CancellationToken.

#### Scenario: Ask call includes both timeout and CT
- **WHEN** an API-layer method calls `Ask<T>` on an actor
- **THEN** it uses `Ask<T>(message, timeout, ct)`, not `Ask<T>(message, timeout)`

#### Scenario: Client disconnects during Ask
- **WHEN** the HTTP client disconnects while an Ask call is in progress
- **THEN** the Ask call is cancelled via the CancellationToken without waiting for the full timeout

### Requirement: Actor-to-actor Ask calls remain timeout-only
Ask calls between actors (not originating from HTTP requests) SHALL continue using the timeout-only overload. These are internal coordination not tied to HTTP request lifecycle.

#### Scenario: Internal actor Ask uses timeout only
- **WHEN** an actor calls `Ask<T>` on another actor as part of internal processing
- **THEN** it uses `Ask<T>(message, timeout)` without a CancellationToken

