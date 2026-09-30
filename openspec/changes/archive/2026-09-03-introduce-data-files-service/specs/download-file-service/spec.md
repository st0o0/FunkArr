## REMOVED Requirements

### Requirement: IDownloadFileOperations interface
**Reason**: Replaced by `IDataFiles` in `FunkArr.Core` which provides the same operations (CreateDirectory, Move, Remove) plus additional capabilities (atomic writes, ReadText, ListFiles, Watch, CanWrite) with built-in Linux permissions handling for all domains.
**Migration**: Replace `IDownloadFileOperations` injection with `IDataFiles`. Method mapping: `EnsureDirectory` -> `CreateDirectory`, `MoveFile` -> `Move`, `DeleteDirectory` -> `Remove`.

### Requirement: DownloadFileOperations implementation
**Reason**: Replaced by `DataFiles` implementation of `IDataFiles` in `FunkArr.Core`.
**Migration**: Remove `DownloadFileOperations` class and `IDownloadFileOperations` interface from `FunkArr.Download`. The `DataFiles` implementation handles permissions via `OperatingSystem.IsLinux()` guard.

### Requirement: EnsureDirectories on startup
**Reason**: Replaced by `DataFiles.CreateDirectory` called from the existing `IHostedLifecycleService`. The startup bootstrapping uses `DataPaths` for path resolution instead of `DownloadOptions.CompletePath`/`IncompletePath`.
**Migration**: Update the startup service to use `IDataFiles.CreateDirectory` with paths from `DataPaths.Complete`, `DataPaths.Incomplete`, and category subdirectories.
