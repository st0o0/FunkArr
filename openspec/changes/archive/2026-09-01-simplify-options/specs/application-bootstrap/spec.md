## MODIFIED Requirements

### Requirement: FunkArrOptions binding
`FunkArrServiceSetup` SHALL register `FunkArrOptions` bound to config section `"FunkArr"`
with `.ValidateOnStart()`. `FunkArrOptions` SHALL be defined in `FunkArr.Core` and have properties:
- `ApiKey` (string, default `"funkarr-default-api-key"`)
- `DataPath` (string, default `"data"`) — the single configurable storage root

`FunkArrOptions` SHALL expose the following computed read-only properties:
- `PersistencePath` — returns `Path.Combine(DataPath, "funkarr.db")`
- `DownloadPath` — returns `Path.Combine(DataPath, "downloads")`
- `RuleSetDataPath` — returns `Path.Combine(DataPath, "community")`

These computed properties SHALL NOT be bindable from configuration. Only `DataPath` SHALL be configurable.

`RuleSetUpdaterOptions` SHALL NOT have a `DataPath` property. Consumers needing the ruleset data path SHALL use `FunkArrOptions.RuleSetDataPath`.

#### Scenario: Default options bind without configuration
- **WHEN** no `FunkArr` section exists in configuration
- **THEN** `FunkArrOptions.ApiKey` equals `"funkarr-default-api-key"` and `DataPath` equals `"data"`

#### Scenario: Computed paths derive from DataPath
- **WHEN** `FunkArrOptions.DataPath` is `"/data"`
- **THEN** `PersistencePath` equals `"/data/funkarr.db"`, `DownloadPath` equals `"/data/downloads"`, and `RuleSetDataPath` equals `"/data/community"`

#### Scenario: Environment variable overrides DataPath
- **WHEN** `FunkArr__DataPath` is set to `"/custom/path"`
- **THEN** `FunkArrOptions.DataPath` equals `"/custom/path"` and all computed paths derive from it

#### Scenario: Environment variable overrides ApiKey
- **WHEN** `FunkArr__ApiKey` is set to `"custom-key"`
- **THEN** `FunkArrOptions.ApiKey` equals `"custom-key"`

### Requirement: Configuration files
The host SHALL load configuration from:
1. `appsettings.json` (required, production defaults)
2. `appsettings.Development.json` (optional, dev overrides)
3. Environment variables

`appsettings.json` SHALL contain sensible defaults including the `FunkArr` section
with `ApiKey` and `DataPath`, and Serilog minimum level set to `Information`
with `Microsoft.AspNetCore` override to `Warning`.

#### Scenario: Development overrides apply
- **WHEN** running in Development environment
- **THEN** `appsettings.Development.json` values override `appsettings.json`

#### Scenario: Environment variables override all files
- **WHEN** `FunkArr__ApiKey` is set as an environment variable
- **THEN** it takes precedence over the value in appsettings.json

## REMOVED Requirements

### Requirement: PersistencePath configuration
**Reason**: Replaced by computed property derived from `DataPath`. `PersistencePath` is no longer independently configurable.
**Migration**: Set `FunkArr__DataPath` instead. Database file is always at `{DataPath}/funkarr.db`.
