## MODIFIED Requirements

### Requirement: SABnzbd services return domain results
`SabnzbdDownloadService` and `SabnzbdQueueService` SHALL wrap all `Ask<>` calls in try/catch blocks. `TimeoutException` SHALL be caught and translated to `SabnzbdResult.Error("Request timed out", 504)`. Other exceptions SHALL be caught and translated to `SabnzbdResult.Error` with an appropriate message and status code. No `Ask<>` call SHALL propagate exceptions to the controller.

#### Scenario: Actor timeout in download service
- **WHEN** `SabnzbdDownloadService` calls `Ask<>` and the actor does not respond within the timeout
- **THEN** the service returns `SabnzbdResult.Error("Request timed out", 504)`
- **THEN** no exception propagates to the controller

#### Scenario: Actor timeout in queue service
- **WHEN** `SabnzbdQueueService` calls `Ask<>` and the actor does not respond within the timeout
- **THEN** the service returns `SabnzbdResult.Error("Request timed out", 504)`

#### Scenario: Unexpected exception in service
- **WHEN** an `Ask<>` call throws a non-timeout exception
- **THEN** the service logs the exception and returns `SabnzbdResult.Error` with a generic message

### Requirement: Newznab search service returns domain result
`NewznabSearchService` SHALL differentiate exception types in its catch block. `TimeoutException` SHALL produce `SearchServiceResult.Failed("Search timed out")`. Other exceptions SHALL produce `SearchServiceResult.Failed("Search failed")` with a distinct message. Both SHALL be logged.

#### Scenario: Timeout during Newznab search
- **WHEN** the search actor does not respond within the timeout
- **THEN** the service returns `SearchServiceResult.Failed("Search timed out")`
- **THEN** the exception is logged at Warning level

#### Scenario: Non-timeout error during Newznab search
- **WHEN** the search actor throws a non-timeout exception
- **THEN** the service returns `SearchServiceResult.Failed("Search failed")`
- **THEN** the exception is logged at Error level

### Requirement: SABnzbd queue service guards response types
`SabnzbdQueueService.GetHistory()` SHALL verify the response type from `Ask<>` before casting. If the response is not the expected type, it SHALL return `SabnzbdResult.Error("History query failed", 502)` instead of throwing an `InvalidCastException`.

#### Scenario: Unexpected response type from history query
- **WHEN** `GetHistory()` receives an unexpected response type from the actor
- **THEN** the service returns `SabnzbdResult.Error("History query failed", 502)`
- **THEN** no `InvalidCastException` is thrown
