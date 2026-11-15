## Purpose

Self-URL resolution for setup endpoints — determines how external Arr services reach FunkArr. Configurable via `FunkArrOptions.BaseUrl` with per-request override and request-context fallback.

## ADDED Requirements

### Requirement: FunkArrOptions.BaseUrl configuration property
`FunkArrOptions` SHALL include a `BaseUrl` property of type `string` with an empty default. When set (non-empty), it SHALL be used as the default self-URL for setup proxy endpoints and SHALL be exposed in the health check `connectionInfo`.

#### Scenario: BaseUrl configured via environment variable
- **WHEN** the environment variable `FunkArr__BaseUrl` is set to `http://funkarr:6969`
- **THEN** `FunkArrOptions.BaseUrl` SHALL resolve to `http://funkarr:6969`

#### Scenario: BaseUrl empty by default
- **WHEN** no `FunkArr__BaseUrl` is configured
- **THEN** `FunkArrOptions.BaseUrl` SHALL be an empty string

### Requirement: Self-URL resolution chain
Setup proxy endpoints SHALL resolve the self-URL using a three-tier chain: (1) `CreateArrResourceRequest.FunkArrUrl` if non-empty, (2) `FunkArrOptions.BaseUrl` if non-empty, (3) `ctx.Request.Scheme://ctx.Request.Host` as fallback. The resolved URL SHALL be trimmed of trailing slashes.

#### Scenario: Per-request override takes priority
- **WHEN** `CreateArrResourceRequest.FunkArrUrl` is `http://custom:1234`
- **AND** `FunkArrOptions.BaseUrl` is `http://funkarr:6969`
- **THEN** the resolved self-URL SHALL be `http://custom:1234`

#### Scenario: Config BaseUrl used when no per-request override
- **WHEN** `CreateArrResourceRequest.FunkArrUrl` is null or empty
- **AND** `FunkArrOptions.BaseUrl` is `http://funkarr:6969`
- **THEN** the resolved self-URL SHALL be `http://funkarr:6969`

#### Scenario: Request context fallback
- **WHEN** both `CreateArrResourceRequest.FunkArrUrl` and `FunkArrOptions.BaseUrl` are empty
- **AND** the HTTP request has scheme `http` and host `localhost:6969`
- **THEN** the resolved self-URL SHALL be `http://localhost:6969`

### Requirement: Self-URL validation
The resolved self-URL SHALL be validated as an absolute URI with `http` or `https` scheme before use. If validation fails, the endpoint SHALL return an `ArrResourceResponse` with `Success=false` and an error message indicating the URL is invalid.

#### Scenario: Valid HTTP URL passes validation
- **WHEN** the resolved self-URL is `http://funkarr:6969`
- **THEN** validation SHALL pass and the endpoint SHALL proceed with payload creation

#### Scenario: Valid HTTPS URL passes validation
- **WHEN** the resolved self-URL is `https://funkarr.example.com`
- **THEN** validation SHALL pass

#### Scenario: Malformed URL returns error
- **WHEN** the resolved self-URL is `not-a-url`
- **THEN** the endpoint SHALL return `ArrResourceResponse` with `Success=false` and `Error` containing "Invalid FunkArr URL"

#### Scenario: Non-HTTP scheme returns error
- **WHEN** the resolved self-URL is `ftp://funkarr:6969`
- **THEN** the endpoint SHALL return `ArrResourceResponse` with `Success=false`

### Requirement: Shared resolution helper
All five setup proxy endpoints SHALL use a single shared static method for self-URL resolution and validation, eliminating code duplication.

#### Scenario: All endpoints use the same resolution logic
- **WHEN** any of the five setup proxy endpoints processes a request
- **THEN** it SHALL call the shared resolution method with the request, options, and HTTP context
