## Why

The app version is read through two unrelated mechanisms: a `VersionInfo.Version` const generated into every non-test assembly by a target in `Directory.Build.props`, and `Assembly.GetName().Version` in three host setup containers. They print different strings for the same build (`0.1.0.0` in logs, `0.1.0` in telemetry and the GitHub User-Agent) and `AssemblyVersion` drops prerelease suffixes. The const also exists as an `internal` copy per assembly.

## What Changes

- Generate one version const from `$(InformationalVersion)` (fallback `$(Version)`) in `FunkArr.Core` only, as a `public` class.
- Remove the `GenerateVersionInfo` target from `src/Directory.Build.props`.
- `SystemApiEndpoints` (`/api/system/version`) reads the new const instead of the per-assembly `VersionInfo`.
- `LoggingSetupContainer`, `TelemetrySetupContainer` and `RuleSetSetupContainer` use the new const instead of `Assembly.GetName().Version`.
- Verify the generated file at build time: whether `InformationalVersion` is already resolved before `CoreCompile`, and how a SourceLink `+sha` suffix appears.
- Out of scope: commit split (`Semantic`/`Commit`), a UI `useAppVersion()` composable, the SABnzbd fake version `4.3.3`, ruleset versioning.

## Capabilities

### New Capabilities
- `app-version`: single build-time source of the application version, exposed from `FunkArr.Core` and consumed by logging, telemetry, the GitHub User-Agent and the version endpoint.

### Modified Capabilities
- `logging-setup-container`: `ApplicationVersion` enrichment uses the shared version const instead of the entry assembly version.

## Impact

- `src/Directory.Build.props`, `src/FunkArr.Core/FunkArr.Core.csproj`
- `src/FunkArr.Api/SystemApiEndpoints.cs`
- `src/FunkArr/Configuration/{LoggingSetupContainer,TelemetrySetupContainer,RuleSetSetupContainer}.cs`
- Log, telemetry and User-Agent strings change format (for example `0.1.0.0` becomes `0.1.0`). No API contract change: `VersionResponse.AppVersion` stays a string.
- Other projects keep referencing only Core, so the domain-isolation rules are unaffected.
