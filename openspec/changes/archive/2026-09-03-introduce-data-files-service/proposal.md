## Why

File I/O is scattered across Download, RuleSet, and Health domains with no shared pattern for permissions, atomic writes, or error handling. Path computation is fragmented across `FunkArrOptions` computed properties, `DownloadPaths.Compute()`, and inline `Path.Combine()` calls in actors. This causes permission failures in Docker (root vs nobody), makes actors untestable without a real filesystem, and blocks the upcoming local rulesets feature which needs the same atomic write pattern the RuleSetUpdater already does manually.

## What Changes

- Introduce `IDataFiles` service in Core — unified file I/O with built-in permissions (Linux chmod), atomic writes, safe deletes, and `FileSystemWatcher` factory. Built on `System.IO.Abstractions` for testability with `MockFileSystem`.
- Introduce `DataPaths` record in Core — all path resolution computed once at startup from two configurable roots (`DataPath`, `Download:Path`). Convention-based directory structure replaces scattered computed properties.
- **BREAKING**: Remove `IDownloadFileOperations` and `DownloadFileOperations` — replaced by `IDataFiles`.
- **BREAKING**: Remove `DownloadPaths` record — path resolution absorbed into `DataPaths.ResolveDownload()`.
- **BREAKING**: Remove `FunkArrOptions.PersistencePath`, `.RuleSetDataPath`, `.LocalRuleSetDataPath` — replaced by `DataPaths` conventions.
- **BREAKING**: Rename `DownloadOptions.DownloadPath` to `Path` (env: `FunkArr__Download__Path`). Remove `CompletePath`/`IncompletePath` computed properties.
- Replace all direct `File.*` / `Directory.*` calls in actors with `IDataFiles`.
- Add `System.IO.Abstractions` NuGet to Core, `TestingHelpers` to test projects.

## Capabilities

### New Capabilities
- `data-files`: Unified file I/O service with permissions, atomic writes, safe deletes, and watcher factory
- `data-paths`: Convention-based path resolution for all data locations from two configurable roots

### Modified Capabilities
- `download-file-service`: Replaced by `data-files` — same operations, broader scope
- `download-path-resolution`: Absorbed into `data-paths` — `DownloadPaths` record removed, logic moves to `DataPaths.ResolveDownload()`
- `download-options`: `DownloadPath` renamed to `Path`, `CompletePath`/`IncompletePath` removed
- `download-worker`: Injects `IDataFiles` + `DataPaths` instead of `IDownloadFileOperations`
- `download-messages`: `RecordDownload` path field stays, computed from `DataPaths`
- `ruleset-filewatcher`: Uses `IDataFiles.Watch()` instead of manual `FileSystemWatcher`
- `ruleset-updater`: Uses `IDataFiles` for atomic directory replacement and file writes
- `ruleset-management`: Uses `IDataFiles` for scanning and reading rulesets
- `setup-health-check`: Uses `IDataFiles.CanWrite()` + `DataPaths` for directory checks
- `download-manager`: Uses `DataPaths` for persistence path resolution

## Impact

- **Core**: New `IDataFiles` interface, `DataFiles` implementation, `DataPaths` class. New NuGet dependency: `System.IO.Abstractions`.
- **Download domain**: `IDownloadFileOperations`, `DownloadFileOperations`, `DownloadPaths` deleted. `DownloadWorker` updated.
- **RuleSet domain**: `RuleSetManager`, `RuleSetWorker`, `RuleSetUpdater` updated to use `IDataFiles` + `DataPaths`.
- **Api**: `SetupApiEndpoints` health checks updated.
- **Host**: `ServiceSetupContainer` registers `DataPaths` + `IDataFiles`. `AkkaSetupContainer` uses `DataPaths.Database`.
- **Config**: `FunkArrOptions` simplified (only `ApiKey` + `DataPath`). `DownloadOptions` property rename.
- **Docker**: `FunkArr__Download__DownloadPath` env var becomes `FunkArr__Download__Path`.
- **Tests**: All domain tests gain `MockFileSystem` — no more real filesystem in unit tests.
