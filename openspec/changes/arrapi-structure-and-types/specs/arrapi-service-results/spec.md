## MODIFIED Requirements

### Requirement: SABnzbd services return domain results
SABnzbd queue and download services SHALL return `SabnzbdResult` with typed response data. `SabnzbdResult.Ok<T>` SHALL be generic and wrap named record types; no anonymous types SHALL be returned or serialized. Services SHALL use `SabnzbdVersionResponse(string Version)` for version responses, `SabnzbdStatusResponse(bool Status)` for plain success, `SabnzbdAddResponse(bool Status, IReadOnlyList<string> NzoIds)` for addfile, a typed config record for `get_config`, and `SabnzbdErrorResponse(bool Status, string Error)` for error responses, including the API-key error. Services SHALL wrap all `Ask<>` calls in try/catch blocks. `TimeoutException` SHALL be caught and translated to `SabnzbdResult.Error("Request timed out", 504)`. Other exceptions SHALL be caught and translated to `SabnzbdResult.Error` with an appropriate message and status code. No `Ask<>` call SHALL propagate exceptions to the controller.

#### Scenario: Version response is typed
- **WHEN** the version mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping `SabnzbdVersionResponse(SabnzbdConstants.Version)`

#### Scenario: Error response is typed
- **WHEN** an invalid mode or missing parameter is detected, or the API key is wrong
- **THEN** the response SHALL be produced from `SabnzbdErrorResponse(false, message)`, not from an anonymous object

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
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a typed config record whose JSON is identical to the previous anonymous structure

#### Scenario: Pause and resume responses are typed
- **WHEN** pause or resume mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping `SabnzbdStatusResponse`

#### Scenario: Slot status and priority are enums
- **WHEN** a queue slot or history slot is built
- **THEN** `status` and `priority` SHALL be ArrApi enums with `Unknown = 0` and `[JsonStringEnumMemberName]` wire names, serialized to the same strings as before, and mapped from Messages enums via exhaustive `switch`

#### Scenario: GetQueue actor failure returns Error
- **WHEN** the actor does not return a `QueueResult`
- **THEN** it SHALL return `SabnzbdResult.Error` with message and status code 502

#### Scenario: AddFile returns Ok with nzo_ids
- **WHEN** `SabnzbdDownloadService.AddFile()` succeeds
- **THEN** it SHALL return `SabnzbdResult.Ok` wrapping `SabnzbdAddResponse` with `nzo_ids`

#### Scenario: AddFile with missing file returns Error
- **WHEN** `SabnzbdDownloadService.AddFile()` is called with null file
- **THEN** it SHALL return `SabnzbdResult.Error` with message and status code 400

#### Scenario: DeleteFromQueue with invalid GUID returns Error
- **WHEN** `SabnzbdDownloadService.DeleteFromQueue()` is called with non-GUID string
- **THEN** it SHALL return `SabnzbdResult.Error` with "Item not found" message

#### Scenario: JSON output unchanged
- **WHEN** any SABnzbd response is serialized
- **THEN** the JSON SHALL be byte-identical to the output before the typing refactor
