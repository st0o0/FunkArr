## MODIFIED Requirements

### Requirement: SABnzbd services return domain results
SABnzbd queue and download services SHALL return `SabnzbdResult` with typed response data. `SabnzbdResult.Ok` SHALL wrap named record types instead of anonymous types. Services SHALL use `SabnzbdVersionResponse(string Version)` for version responses and `SabnzbdErrorResponse(bool Status, string Error)` for error responses.

#### Scenario: Version response is typed
- **WHEN** the version mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok(new SabnzbdVersionResponse(SabnzbdConstants.Version))`

#### Scenario: Error response is typed
- **WHEN** an invalid mode or missing parameter is detected
- **THEN** the controller SHALL return `SabnzbdResult.Error(message)` or use `new SabnzbdErrorResponse(false, message)` instead of `new { status = false, error = message }`

#### Scenario: AddFile returns Ok with nzo_ids
- **WHEN** a file is successfully added
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a response containing `nzo_ids`

#### Scenario: Queue config is typed
- **WHEN** get_config mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a typed config record

#### Scenario: Queue and history responses are typed
- **WHEN** queue or history mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping `QueueResponse` or `HistoryResponse`

#### Scenario: FullStatus response is typed
- **WHEN** fullstatus mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping `FullStatusResponse`

#### Scenario: Pause and resume responses are typed
- **WHEN** pause or resume mode is requested
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a typed status response

### Requirement: Controllers own HTTP mapping
Controllers SHALL map `SabnzbdResult` variants to HTTP responses. `SabnzbdResult.Ok` SHALL map to `Ok(result.Data)`. `SabnzbdResult.Error` SHALL map to an `ObjectResult` with a `SabnzbdErrorResponse` record, not an anonymous type.

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
