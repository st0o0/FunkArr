# arr-setup-client-tests Specification

## Purpose

Test coverage for `ArrSetupClient` response parsing and error handling using stubbed HTTP handlers.

## Requirements

### Requirement: ArrSetupClient response parsing is tested
The `ArrSetupClient` SHALL have unit tests covering all response parsing paths using a stubbed `HttpMessageHandler`.

#### Scenario: Success response extracts id and name
- **WHEN** the stub returns HTTP 201 with `{"id":42,"name":"FunkArr"}`
- **THEN** the response SHALL have `Success=true`, `Id=42`, `Name="FunkArr"`

#### Scenario: Success response with provider message
- **WHEN** the stub returns HTTP 201 with a `message` object containing `{"message":"Test warning","type":"warning"}`
- **THEN** the response SHALL include an `ArrProviderMessage` with matching fields

#### Scenario: Validation error array
- **WHEN** the stub returns HTTP 400 with `[{"propertyName":"BaseUrl","errorMessage":"is required","isWarning":false}]`
- **THEN** the response SHALL have `Success=false` and `ValidationErrors` with one entry

#### Scenario: General error object
- **WHEN** the stub returns HTTP 500 with `{"message":"Internal error"}`
- **THEN** the response SHALL have `Success=false` and `Error="Internal error"`

#### Scenario: Connection timeout
- **WHEN** the stub throws `TaskCanceledException`
- **THEN** the response SHALL have `Success=false` and `Error` containing "timed out"

#### Scenario: Invalid URL
- **WHEN** `PostResourceAsync` is called with a non-HTTP URL
- **THEN** the response SHALL have `Success=false` and `Error` describing invalid URL
