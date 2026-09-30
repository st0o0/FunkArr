## Why

FunkArr has three independent path settings (`PersistencePath`, `DownloadPath`, `DataPath`) scattered across two projects and four options classes. Users must align them manually. Since FunkArr runs in Docker with a single volume mount at `/data`, all paths should derive from one configurable root.

## What Changes

- **BREAKING**: Remove `PersistencePath` and `DownloadPath` from `FunkArrOptions` — replaced by computed properties derived from `DataPath`
- **BREAKING**: Remove `DataPath` from `RuleSetUpdaterOptions` — derived from `FunkArrOptions.DataPath`
- Add `DataPath` property to `FunkArrOptions` (default `"data"`) as the single configurable root
- Add computed read-only properties on `FunkArrOptions`: `PersistencePath`, `DownloadPath`, `RuleSetDataPath`
- Move `FunkArrOptions` from `FunkArr` (host) to `FunkArr.Core` so all domain projects can resolve paths
- Update all consumers to use `FunkArrOptions` for path resolution instead of domain-specific options
- Simplify `appsettings.json` to reflect the new flat structure

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `application-bootstrap`: `FunkArrOptions` properties change — `PersistencePath` and `DownloadPath` become computed from `DataPath`; options class moves to `FunkArr.Core`

## Impact

- **Options classes**: `FunkArrOptions` moves to `Core`, gains `DataPath` + computed properties, loses configurable `PersistencePath`/`DownloadPath`. `RuleSetUpdaterOptions` loses `DataPath`.
- **Config files**: `appsettings.json` and `appsettings.Development.json` simplified
- **Consumers**: `AkkaSetupContainer`, `RuleSetUpdater`, `DownloadApiEndpoints` switch to `FunkArrOptions` for paths
- **Docker**: Single `FunkArr__DataPath=/data` env var replaces multiple path overrides
- **Tests**: `TestOptionsMonitor` usages may need updating
