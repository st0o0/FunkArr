## 1. Download File Service

- [x] 1.1 Create `IDownloadFileService` interface in `FunkArr.Download` with methods: `ResolveTempPath`, `ResolveOutputPath`, `MoveToComplete`, `CleanupIncomplete`, `EnsureDirectories`
- [x] 1.2 Implement `DownloadFileService` (sealed, injects `IOptions<DownloadOptions>`, `ILogger<DownloadFileService>`) with all path resolution, file move, cleanup, and directory creation logic
- [x] 1.3 Register `IDownloadFileService` as singleton in `DownloadServiceExtensions`

## 2. Default Categories

- [x] 2.1 Add default `Categories` configuration (`tv`, `movies`) to `appsettings.json` under `FunkArr:Download`
- [x] 2.2 Update `ApplicationSetupContainer.EnsureDownloadDirectories` to resolve `IDownloadFileService` and call `EnsureDirectories()` instead of inline `Directory.CreateDirectory`

## 3. DownloadWorker Simplification

- [x] 3.1 Replace `IOptionsMonitor<DownloadOptions>` with `IDownloadFileService` in `DownloadWorker` constructor
- [x] 3.2 Replace `ComputePaths()` with calls to `ResolveTempPath` and `ResolveOutputPath` on the file service
- [x] 3.3 Replace inline `Directory.CreateDirectory`, `File.Move`, and `Directory.Delete` in `HandleStart`, `HandleFfmpegResult`, and `CleanupIncomplete` with file service calls
- [x] 3.4 Remove the private `ComputePaths()` and `CleanupIncomplete()` methods from `DownloadWorker`

## 4. Tests

- [x] 4.1 Add `DownloadFileService` path resolution unit tests: `ResolveTempPath`, `ResolveOutputPath` with various category configurations (known, unknown, null, custom dir)
- [x] 4.2 Add `DownloadFileService.EnsureDirectories` test: verifies correct directories are created for configured categories
- [x] 4.3 Verify existing `DownloadWorker` tests still pass or update them to mock `IDownloadFileService`

## 5. Verification

- [x] 5.1 Run `dotnet build FunkArr.slnx` and `dotnet format --verify-no-changes`
- [x] 5.2 Run all Download test project tests
