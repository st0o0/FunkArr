## RENAMED Requirements

### Requirement: SetupValidationService renamed
- **FROM:** `SetupValidationService` in `Setup/`
- **TO:** `SetupValidator` in `Setup/` (stays in Setup, suffix removed)

### Requirement: ApiKeyValidationService renamed
- **FROM:** `ApiKeyValidationService` in `Setup/`
- **TO:** `ApiKeyValidator` in `Setup/` (stays in Setup, suffix removed)

## MODIFIED Requirements

### Requirement: FunkArr API key self-check
The system SHALL validate that FunkArr's `ApiKey` configuration value is set to a non-empty value via `ApiKeyValidator` (renamed from `ApiKeyValidationService`). The default key (`"funkarr-default-api-key"`) is a valid value.

#### Scenario: API key configured
- **WHEN** `FunkArrOptions.ApiKey` is a non-empty string
- **THEN** the `api-key` self-check SHALL report status `pass`

#### Scenario: API key missing
- **WHEN** `FunkArrOptions.ApiKey` is empty or unset
- **THEN** the `api-key` self-check SHALL report status `fail`
