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
SABnzbd queue and download services SHALL return `SabnzbdResult` with typed response data. `SabnzbdResult.Ok` SHALL wrap named record types instead of anonymous types. Services SHALL use `SabnzbdVersionResponse(string Version)` for version responses and `SabnzbdErrorResponse(bool Status, string Error)` for error responses. Services SHALL wrap all `Ask<>` calls in try/catch blocks. `TimeoutException` SHALL be caught and translated to `SabnzbdResult.Error("Request timed out", 504)`. Other exceptions SHALL be caught and translated to `SabnzbdResult.Error` with an appropriate message and status code. No `Ask<>` call SHALL propagate exceptions to the controller.

#### Scenario: Version response is typed
- **WHEN** the version mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok(new SabnzbdVersionResponse(SabnzbdConstants.Version))`

#### Scenario: Error response is typed
- **WHEN** an invalid mode or missing parameter is detected
- **THEN** the controller SHALL return `SabnzbdResult.Error(message)` or use `new SabnzbdErrorResponse(false, message)` instead of `new { status = false, error = message }`

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

#### Scenario: Queue and history responses are typed
- **WHEN** queue or history mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping `QueueResponse` or `HistoryResponse`

#### Scenario: FullStatus response is typed
- **WHEN** fullstatus mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping `FullStatusResponse`

#### Scenario: Queue config is typed
- **WHEN** get_config mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a typed config record

#### Scenario: Pause and resume responses are typed
- **WHEN** pause or resume mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a typed status response

#### Scenario: GetQueue actor failure returns Error
- **WHEN** the actor does not return a `QueueResult`
- **THEN** it SHALL return `SabnzbdResult.Error` with message and status code 502

#### Scenario: AddFile returns Ok with nzo_ids
- **WHEN** `SabnzbdDownloadService.AddFile()` succeeds
- **THEN** it SHALL return `SabnzbdResult.Ok` containing a response with `nzo_ids`

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
Controllers SHALL map `SabnzbdResult` variants to HTTP responses. `SabnzbdResult.Ok` SHALL map to `Ok(result.Data)`. `SabnzbdResult.Error` SHALL map to an `ObjectResult` with a `SabnzbdErrorResponse` record, not an anonymous type. `NewznabController` and `SabnzbdController` SHALL pattern-match on service result types and map them to appropriate `IActionResult` responses using `ControllerBase` helper methods where applicable.

#### Scenario: SABnzbd Ok maps to ObjectResult
- **WHEN** a service returns `SabnzbdResult.Ok(data)` where data is a named record
- **THEN** the controller SHALL return `Ok(data)`

#### Scenario: SABnzbd Error maps to typed error
- **WHEN** a service returns `SabnzbdResult.Error`
- **THEN** the controller SHALL return an `ObjectResult` wrapping `new SabnzbdErrorResponse(false, error.Message)` with the appropriate status code

#### Scenario: Newznab success maps to XML
- **WHEN** a search returns `SearchServiceResult.Success`
- **THEN** the controller SHALL return an XML result

#### Scenario: Newznab error maps to XML error
- **WHEN** a search returns `SearchServiceResult.Failed`
- **THEN** the controller SHALL return an XML error result
