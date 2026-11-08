## Purpose

Delta spec for `ArrSetupClient` — fixes URI construction to preserve Arr sub-paths.

## MODIFIED Requirements

### Requirement: Setup client provisions Arr resources
The `ArrSetupClient` SHALL send POST requests to Sonarr, Radarr, and Prowlarr APIs to create indexer and download client resources. The client SHALL construct the target URL by concatenating the base URL (trimmed of trailing slashes) with the API path, preserving any sub-path prefix on the base URL. The client SHALL be registered in DI and injected into setup endpoints.

#### Scenario: Successful resource creation
- **WHEN** the client sends a POST request and receives a 2xx response
- **THEN** the response SHALL be deserialized into an `ArrResourceResponse` containing the `Id` (int) and `Name` (string) of the created resource

#### Scenario: Response includes provider message
- **WHEN** the Arr app returns a `message` object in the success response
- **THEN** the `ArrResourceResponse` SHALL include an `ArrProviderMessage` with matching fields

#### Scenario: Validation error response
- **WHEN** the Arr app returns HTTP 400 with a JSON array of validation errors
- **THEN** the response SHALL have `Success=false` and `ValidationErrors` with matching entries

#### Scenario: General error response
- **WHEN** the Arr app returns an error with a JSON object containing a `message` field
- **THEN** the response SHALL have `Success=false` and `Error` matching the message

#### Scenario: Connection failure
- **WHEN** the HTTP request fails due to timeout or connection error
- **THEN** the response SHALL contain the error message without throwing an exception

#### Scenario: Sub-path base URL preserved
- **WHEN** the base URL is `http://host:8989/sonarr` and the API path is `/api/v3/indexer`
- **THEN** the target URL SHALL be `http://host:8989/sonarr/api/v3/indexer`

#### Scenario: Base URL with trailing slash
- **WHEN** the base URL is `http://host:8989/sonarr/` and the API path is `/api/v3/indexer`
- **THEN** the target URL SHALL be `http://host:8989/sonarr/api/v3/indexer` (no double slash)

#### Scenario: Base URL without sub-path
- **WHEN** the base URL is `http://host:8989` and the API path is `/api/v3/indexer`
- **THEN** the target URL SHALL be `http://host:8989/api/v3/indexer`
