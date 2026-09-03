## 1. Messages & Persistence DTOs

- [x] 1.1 Remove `IncompletePath` and `OutputPath` from `InitDownload` record in `FunkArr.Messages/Download/InitDownload.cs`
- [x] 1.2 Remove `IncompletePath` and `OutputPath` from `DownloadInitialized` record in `FunkArr.Persistence/Events/Download/DownloadInitialized.cs`

## 2. Worker State

- [x] 2.1 Remove `IncompletePath` and `OutputPath` from `DownloadWorkerState` record and its `Apply(DownloadInitialized)` extension method in `FunkArr.Download/DownloadWorkerState.cs`

## 3. DownloadWorker

- [x] 3.1 Add `IOptionsMonitor<DownloadOptions>` constructor parameter to `DownloadWorker`
- [x] 3.2 Add private `_incompletePath` and `_outputPath` fields, computed in `HandleStart` from `DownloadOptions` + persisted state (Title, Category, entity id)
- [x] 3.3 Update `HandleInit` to pass only domain metadata to `DownloadInitialized`
- [x] 3.4 Update `HandleStart` to compute paths before creating directories and launching FFmpeg
- [x] 3.5 Update `HandleReset` to persist `DownloadInitialized` without path fields
- [x] 3.6 Update `HandleExited` and `CleanupIncomplete` to use private fields instead of `_state.IncompletePath` / `_state.OutputPath`
- [x] 3.7 Update `HandleQueryStatus` to use private `_outputPath` field
- [x] 3.8 Recompute paths in `OnRecoveryCompleted` for Workers recovering from Downloading state

## 4. DownloadManager

- [x] 4.1 Remove path computation from `HandleAdd` — send `InitDownload` with domain metadata only
- [x] 4.2 Remove `DownloadOptions` usage for path building (keep for `ConcurrentDownloads` and `ResolveCategoryDir` is no longer needed here)

## 5. Tests

- [x] 5.1 Update `DownloadWorkerStateTests` to remove path assertions and reflect new state shape
- [x] 5.2 Update `DownloadManagerStateTests` if affected
- [x] 5.3 Verify build and all tests pass with `dotnet build FunkArr.slnx` and test runners
- [x] 5.4 Run `dotnet format` and fix any violations
