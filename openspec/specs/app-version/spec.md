## Purpose

Single build-time source of the application version, exposed from `FunkArr.Core` and shared by logging, telemetry, the GitHub User-Agent and the version endpoint.

## Requirements

### Requirement: Single build-time application version
`FunkArr.Core` SHALL expose the application version as one generated `public` const, derived at build time from `$(InformationalVersion)` and falling back to `$(Version)` when the former is empty. No other project SHALL generate its own copy.

#### Scenario: Version generated from informational version
- **WHEN** a build sets `/p:Version=0.2.0-rc1`
- **THEN** the const in `FunkArr.Core` contains a value starting with `0.2.0-rc1`

#### Scenario: Fallback to Version
- **WHEN** `$(InformationalVersion)` is empty at the time the target runs
- **THEN** the const contains `$(Version)`

#### Scenario: One copy only
- **WHEN** the solution is built
- **THEN** exactly one generated version source file exists, in the `FunkArr.Core` intermediate output

### Requirement: Consumers use the shared version
The version endpoint, the Serilog `ApplicationVersion` enrichment, the OpenTelemetry service version and the GitHub HTTP client User-Agent SHALL all read the shared const and SHALL NOT use `Assembly.GetName().Version`.

#### Scenario: Same string everywhere
- **WHEN** the application runs a given build
- **THEN** `GET /api/system/version` `appVersion`, the log property `ApplicationVersion`, the OTel `service.version` and the `User-Agent` `FunkArr/<version>` carry the same version string
