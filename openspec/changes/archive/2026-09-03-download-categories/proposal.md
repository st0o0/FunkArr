## Why

Downloads land flat in `complete/` instead of being sorted into category subdirectories. The category routing logic exists (`ResolveCategoryDir`, `ComputePaths`) but `Categories` is never configured, so every download resolves to the root. Additionally, all path resolution, directory creation, file moves, and cleanup are inlined in `DownloadWorker` — making the actor responsible for both business logic and filesystem operations, which is untestable and scattered.

## What Changes

- Introduce `IDownloadFileService` / `DownloadFileService` in `FunkArr.Download` to centralize all download-related filesystem operations (path resolution, directory creation, file moves, cleanup).
- Slim `DownloadWorker` to pure actor logic — delegate all filesystem work to the file service.
- Ship default categories (`tv`, `movies`) in `appsettings.json` so downloads are routed out of the box.
- Ensure category subdirectories are created at startup alongside `complete/` and `incomplete/`.
- Register the file service in DI via `DownloadServiceExtensions`.

## Capabilities

### New Capabilities
- `download-file-service`: Centralized filesystem operations for the download domain — path resolution, directory management, file moves, and cleanup.

### Modified Capabilities
- `download-worker`: DownloadWorker delegates filesystem operations to `IDownloadFileService` instead of inlining `Path.*`, `Directory.*`, `File.*` calls.
- `download-options`: Ships default categories (`tv`, `movies`) in `appsettings.json`. `EnsureDownloadDirectories` at startup also creates category subdirectories.

## Impact

- **FunkArr.Download**: New `IDownloadFileService` interface and `DownloadFileService` implementation. `DownloadWorker` simplified — filesystem code removed, file service injected.
- **FunkArr.Download/DownloadServiceExtensions**: Registers `IDownloadFileService` in DI.
- **FunkArr/Configuration/ApplicationSetupContainer**: `EnsureDownloadDirectories` uses the file service to also create category dirs.
- **FunkArr/appsettings.json**: Default `Categories` configuration added.
- **Tests**: New `DownloadFileService` path resolution tests. Existing `DownloadWorker` tests may need mock for file service.
