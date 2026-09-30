## REMOVED Requirements

### Requirement: Setup validation endpoint authentication
**Reason**: `ApiKeyMiddleware` is removed entirely. No endpoints require authentication.
**Migration**: None — the endpoint is now publicly accessible like all other endpoints.

## MODIFIED Requirements

### Requirement: FunkArr API key self-check
The system SHALL validate that FunkArr's `ApiKey` configuration value is set to a non-empty value. The default key (`"funkarr-default-api-key"`) is a valid value.

#### Scenario: API key configured (default or custom)
- **WHEN** `FunkArrOptions.ApiKey` is a non-empty string (including the default value)
- **THEN** the `api-key` self-check SHALL report status `pass`

#### Scenario: API key missing
- **WHEN** `FunkArrOptions.ApiKey` is empty or unset
- **THEN** the `api-key` self-check SHALL report status `fail` with fix guidance describing how to set `FunkArr__ApiKey`

### Requirement: Supplied credentials are not persisted
Given that `Prowlarr` and `ArrInstances` are removed from `FunkArrOptions`, the validation endpoint SHALL accept these credentials exclusively from the request body and SHALL NOT attempt to read them from options or configuration.

#### Scenario: Validation uses only request-body credentials
- **WHEN** a validation request includes Prowlarr or Arr instance credentials
- **THEN** the system SHALL use them only for the duration of the request and SHALL NOT write them to any options class, configuration file, or persistent store

#### Scenario: Validation without external credentials
- **WHEN** a validation request omits Prowlarr and Arr instance sections
- **THEN** the system SHALL run only self-checks (API key, FFmpeg, paths) and return results without any external connectivity checks
