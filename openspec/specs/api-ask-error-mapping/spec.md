# api-ask-error-mapping Specification

## Purpose
TBD - created by archiving change fix-internal-api-bugs. Update Purpose after archive.
## Requirements
### Requirement: Ask timeouts map to 504
Any `Akka.Actor.AskTimeoutException` or `TimeoutException` thrown while handling an `/api` request SHALL produce HTTP 504 with a ProblemDetails body titled "Gateway Timeout", and SHALL be logged at warning level with the endpoint name.

#### Scenario: Actor does not answer
- **WHEN** the DownloadManager does not answer `QueryQueue` within the Ask timeout
- **THEN** `GET /api/downloads/queue` returns 504 with title "Gateway Timeout"

### Requirement: Unexpected actor responses map to 500
When an endpoint receives an actor response of a type it does not handle, it SHALL return HTTP 500 and log an error naming the response type. It SHALL NOT return 504 for this case.

#### Scenario: Unhandled response type
- **WHEN** an actor replies to an endpoint's Ask with a message type the endpoint's switch does not handle
- **THEN** the response is 500
- **AND** an error is logged containing the response type name

