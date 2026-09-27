# arr-setup-client Specification

## Purpose

Typed HTTP client for provisioning indexers and download clients in Sonarr/Radarr/Prowlarr during the setup wizard, with modeled request payloads and response types.

## Requirements

### Requirement: Setup client provisions Arr resources
The `ArrSetupClient` SHALL send POST requests to Sonarr, Radarr, and Prowlarr APIs to create indexer and download client resources. The client SHALL be registered in DI and injected into setup endpoints.

#### Scenario: Successful resource creation
- **WHEN** the client sends a POST request and receives a 2xx response
- **THEN** the response SHALL be deserialized into an `ArrResourceResponse` containing the `Id` (int) and `Name` (string) of the created resource

#### Scenario: Response includes provider message
- **WHEN** the Arr app returns a `message` object in the success response
- **THEN** the `ArrResourceResponse` SHALL include an `ArrProviderMessage` with `Message` and `Type` fields

#### Scenario: Validation error response
- **WHEN** the Arr app returns HTTP 400 with a JSON array of validation errors
- **THEN** the response SHALL be deserialized into `ArrResourceResponse` with `Errors` containing `ArrValidationError` records with `PropertyName`, `ErrorMessage`, and `IsWarning` fields

#### Scenario: General error response
- **WHEN** the Arr app returns an error with a JSON object containing a `message` field
- **THEN** the response SHALL extract the error message into `ArrResourceResponse.Error`

#### Scenario: Connection failure
- **WHEN** the HTTP request fails due to timeout or connection error
- **THEN** the response SHALL contain the error message without throwing an exception

### Requirement: Typed request payloads use non-generic ArrField
The Arr API field payloads SHALL use a non-generic `ArrField(string Name, object Value)` record. The generic type parameter SHALL be removed since it provides no compile-time benefit when always instantiated as `ArrField<object>`.

#### Scenario: Payload fields serialize correctly
- **WHEN** a payload containing `ArrField` values with string, int, and int[] values is serialized
- **THEN** each field value SHALL serialize to its runtime JSON type (string, number, array)

### Requirement: Client is named ArrSetupClient
The HTTP client class SHALL be named `ArrSetupClient` to reflect its exclusive use in the setup provisioning flow.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `ArrSetupClient` SHALL be registered as a typed HttpClient via `services.AddHttpClient<ArrSetupClient>()`
