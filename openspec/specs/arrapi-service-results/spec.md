# Capability: ArrApi Service Results

## Purpose

Defines domain result types for ArrApi services, ensuring services return typed results instead of `IActionResult` and controllers own all HTTP-response mapping.

## Requirements

### Requirement: Newznab search service returns domain result
`NewznabSearchService` SHALL differentiate exception types in its catch block. `TimeoutException` SHALL produce `SearchServiceResult.Failed("Search timed out")`. Other exceptions SHALL produce `SearchServiceResult.Failed("Search failed")` with a distinct message. Both SHALL be logged.

#### Scenario: Successful search returns Ok with Rss
- **WHEN** `NewznabSearchService.Search()` completes successfully
- **THEN** it SHALL return `SearchServiceResult.Ok` containing the `Rss` object

#### Scenario: Empty query returns Empty
- **WHEN** the search command cannot be built (no valid search type)
- **THEN** it SHALL return `SearchServiceResult.Empty` with the requested offset

#### Scenario: Timeout during Newznab search
- **WHEN** the search actor does not respond within the timeout
- **THEN** the service returns `SearchServiceResult.Failed("Search timed out")`
- **THEN** the exception is logged at Warning level

#### Scenario: Non-timeout error during Newznab search
- **WHEN** the search actor throws a non-timeout exception
- **THEN** the service returns `SearchServiceResult.Failed("Search failed")`
- **THEN** the exception is logged at Error level

### Requirement: NZB service returns domain result
`NzbService.GetNzb()` SHALL return a `NzbGetResult` (abstract sealed record) instead of `IActionResult`. Subtypes: `NzbGetResult.Ok(byte[] Content, string FileName)` for successful NZB generation, `NzbGetResult.Error(NewznabError Error)` for validation failures.

#### Scenario: Valid NZB ID returns Ok with bytes
- **WHEN** `NzbService.GetNzb()` is called with a valid Base64-encoded NZB ID
- **THEN** it SHALL return `NzbGetResult.Ok` containing the serialized NZB XML bytes and a filename

#### Scenario: Missing ID returns Error
- **WHEN** `NzbService.GetNzb()` is called with null or empty ID
- **THEN** it SHALL return `NzbGetResult.Error` with `NewznabError.MissingParameter`

#### Scenario: Invalid Base64 returns Error
- **WHEN** `NzbService.GetNzb()` is called with non-Base64 string
- **THEN** it SHALL return `NzbGetResult.Error` with `NewznabError.IncorrectParameter`

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

#### Scenario: GetQueue returns Ok with queue data
- **WHEN** `SabnzbdQueueService.GetQueue()` succeeds
- **THEN** it SHALL return `SabnzbdResult.Ok` containing the queue response data

#### Scenario: GetQueue actor failure returns Error
- **WHEN** the actor does not return a `QueueResult`
- **THEN** it SHALL return `SabnzbdResult.Error` with message and status code 502

#### Scenario: AddFile returns Ok with nzo_ids
- **WHEN** `SabnzbdDownloadService.AddFile()` succeeds
- **THEN** it SHALL return `SabnzbdResult.Ok` containing `{ status = true, nzo_ids = [...] }`

#### Scenario: AddFile with missing file returns Error
- **WHEN** `SabnzbdDownloadService.AddFile()` is called with null file
- **THEN** it SHALL return `SabnzbdResult.Error` with message and status code 400

#### Scenario: DeleteFromQueue with invalid GUID returns Error
- **WHEN** `SabnzbdDownloadService.DeleteFromQueue()` is called with non-GUID string
- **THEN** it SHALL return `SabnzbdResult.Error` with "Item not found" message

### Requirement: SABnzbd queue service guards response types
`SabnzbdQueueService.GetHistory()` SHALL verify the response type from `Ask<>` before casting. If the response is not the expected type, it SHALL return `SabnzbdResult.Error("History query failed", 502)` instead of throwing an `InvalidCastException`.

#### Scenario: Unexpected response type from history query
- **WHEN** `GetHistory()` receives an unexpected response type from the actor
- **THEN** the service returns `SabnzbdResult.Error("History query failed", 502)`
- **THEN** no `InvalidCastException` is thrown

### Requirement: Services have no MVC dependency
Service classes in `FunkArr.ArrApi` SHALL NOT reference types from `Microsoft.AspNetCore.Mvc` namespace. All HTTP-response construction SHALL be the responsibility of controller classes.

#### Scenario: NewznabSearchService has no MVC using
- **WHEN** examining `NewznabSearchService.cs`
- **THEN** it SHALL NOT contain `using Microsoft.AspNetCore.Mvc`

#### Scenario: SabnzbdQueueService has no MVC using
- **WHEN** examining `SabnzbdQueueService.cs`
- **THEN** it SHALL NOT contain `using Microsoft.AspNetCore.Mvc`

#### Scenario: SabnzbdDownloadService has no MVC using
- **WHEN** examining `SabnzbdDownloadService.cs`
- **THEN** it SHALL NOT contain `using Microsoft.AspNetCore.Mvc`

#### Scenario: NzbService has no MVC using
- **WHEN** examining `NzbService.cs`
- **THEN** it SHALL NOT contain `using Microsoft.AspNetCore.Mvc`

### Requirement: Controllers own HTTP mapping
`NewznabController` and `SabnzbdController` SHALL pattern-match on service result types and map them to appropriate `IActionResult` responses using `ControllerBase` helper methods where applicable.

#### Scenario: Newznab search Ok mapped to XML result
- **WHEN** `NewznabSearchService` returns `SearchServiceResult.Ok`
- **THEN** the controller SHALL return `NewznabXmlResult.From(result.Rss)`

#### Scenario: Newznab search Failed mapped to XML error
- **WHEN** `NewznabSearchService` returns `SearchServiceResult.Failed`
- **THEN** the controller SHALL return `NewznabXmlResult.Error(...)`

#### Scenario: SABnzbd Ok mapped to Ok helper
- **WHEN** a SABnzbd service returns `SabnzbdResult.Ok`
- **THEN** the controller SHALL return `Ok(result.Data)`

#### Scenario: SABnzbd Error mapped to status code
- **WHEN** a SABnzbd service returns `SabnzbdResult.Error`
- **THEN** the controller SHALL return an `ObjectResult` with the error message and the specified status code
