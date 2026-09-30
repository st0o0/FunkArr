## MODIFIED Requirements

### Requirement: Version endpoint
The version endpoint SHALL return a typed `SabnzbdVersionResponse` record serialized as `{"version":"4.3.3"}`.

#### Scenario: Valid API key returns version
- **WHEN** a GET request with `mode=version` and a valid API key is received
- **THEN** the response SHALL be `{"version":"4.3.3"}` produced from `new SabnzbdVersionResponse(SabnzbdConstants.Version)`, not an anonymous type

#### Scenario: No API key returns unauthorized
- **WHEN** a GET request with `mode=version` and no API key is received
- **THEN** the response SHALL be 401 Unauthorized

### Requirement: Unknown mode
The unknown mode handler SHALL return a typed `SabnzbdErrorResponse` record.

#### Scenario: Invalid mode returns error
- **WHEN** a GET request with an unrecognized mode is received
- **THEN** the response SHALL be `{"status":false,"error":"Invalid mode"}` produced from `new SabnzbdErrorResponse(false, "Invalid mode")`, not an anonymous type

#### Scenario: Invalid queue command returns error
- **WHEN** a GET request with `mode=queue` and an unrecognized `name` parameter is received
- **THEN** the response SHALL be `{"status":false,"error":"Invalid queue command"}` produced from a typed record
