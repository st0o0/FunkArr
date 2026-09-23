# Capability: ArrApi Service Results

## Purpose

Defines domain result types for ArrApi services, ensuring services return typed results instead of `IActionResult` and controllers own all HTTP-response mapping.

## Requirements

### Requirement: Newznab search service returns domain result
`NewznabSearchService.Search()` SHALL return a `SearchServiceResult` (abstract sealed record) instead of `IActionResult`. Subtypes: `SearchServiceResult.Ok(Rss Rss)` for successful search, `SearchServiceResult.Empty(int Offset)` for no-query searches, `SearchServiceResult.Failed(string Message)` for errors.

#### Scenario: Successful search returns Ok with Rss
- **WHEN** `NewznabSearchService.Search()` completes successfully
- **THEN** it SHALL return `SearchServiceResult.Ok` containing the `Rss` object

#### Scenario: Empty query returns Empty
- **WHEN** the search command cannot be built (no valid search type)
- **THEN** it SHALL return `SearchServiceResult.Empty` with the requested offset

#### Scenario: Actor timeout returns Failed
- **WHEN** the actor ask times out or throws
- **THEN** it SHALL return `SearchServiceResult.Failed` with an error message

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
`SabnzbdQueueService` and `SabnzbdDownloadService` methods SHALL return domain result types instead of `IActionResult`. A shared `SabnzbdResult` (abstract sealed record) with subtypes `SabnzbdResult.Ok(object Data)` and `SabnzbdResult.Error(string Message, int StatusCode)` SHALL cover all SABnzbd operations.

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
