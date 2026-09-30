## 1. NuGet Dependencies

- [x] 1.1 Add `System.IO.Abstractions` to `Directory.Packages.props`
- [x] 1.2 Add `System.IO.Abstractions.TestingHelpers` to `Directory.Packages.props`
- [x] 1.3 Reference `System.IO.Abstractions` in `FunkArr.Core.csproj`
- [x] 1.4 Reference `System.IO.Abstractions.TestingHelpers` in all test `.csproj` files

## 2. DataPaths

- [x] 2.1 Create `DataPaths` class in `FunkArr.Core` with data root paths (`DataRoot`, `Database`, `CommunityRuleSets`, `LocalRuleSets`, `RuleSetVersion`, `Temp`)
- [x] 2.2 Add download root paths (`DownloadRoot`, `Incomplete`, `Complete`) to `DataPaths`
- [x] 2.3 Add `ResolveDownload(entityId, title, category, categories)` method returning `ResolvedDownload` record — absorb logic from `DownloadPaths.Compute()`, `HasEpisodeIdentifier`, `ResolveCategoryDir`
- [x] 2.4 Write `DataPathsTests` — verify all convention paths, `ResolveDownload` with episodes, dates, movies, categories, null/unknown category, custom Dir

## 3. DownloadOptions Rename

- [x] 3.1 Rename `DownloadOptions.DownloadPath` to `Path`, remove `CompletePath` and `IncompletePath` computed properties
- [x] 3.2 Update `docker-compose.dev.yml` env var from `FunkArr__Download__DownloadPath` to `FunkArr__Download__Path`
- [x] 3.3 Update `DownloadOptionsTests` for the renamed property

## 4. IDataFiles Interface and Implementation

- [x] 4.1 Create `IDataFiles` interface in `FunkArr.Core` with all 11 methods
- [x] 4.2 Create `DataFiles` implementation in `FunkArr.Core` using `IFileSystem` — `CreateDirectory`, `Remove`, `Move`, `ReadText`, `WriteText`, `Exists`, `ListFiles`
- [x] 4.3 Implement `WriteAtomic` (temp file + rename) and `ReplaceDirectory` (atomic swap with rollback)
- [x] 4.4 Implement `CanWrite` (test file create+delete) and `Watch` (FileSystemWatcher factory)
- [x] 4.5 Add Linux permission handling (`OperatingSystem.IsLinux()` guard, dir mode 777, file mode 666)
- [x] 4.6 Write `DataFilesTests` using `MockFileSystem` — cover all 11 methods, safe remove, atomic write failure cleanup, replace directory rollback, non-existent dir handling

## 5. DI Registration

- [x] 5.1 Register `IFileSystem` → `FileSystem` as singleton in `ServiceSetupContainer`
- [x] 5.2 Register `DataPaths` as singleton (from `IOptions<FunkArrOptions>` + `IOptions<DownloadOptions>`)
- [x] 5.3 Register `IDataFiles` → `DataFiles` as singleton

## 6. Simplify FunkArrOptions

- [x] 6.1 Remove `PersistencePath`, `RuleSetDataPath`, `LocalRuleSetDataPath` from `FunkArrOptions`
- [x] 6.2 Update `AkkaSetupContainer` to use `DataPaths.Database` instead of `FunkArrOptions.PersistencePath`

## 7. Migrate Download Domain

- [x] 7.1 Delete `IDownloadFileOperations`, `DownloadFileOperations`, `DownloadPaths` — remove DI registration in `DownloadServiceExtensions`
- [x] 7.2 Update `DownloadWorker` — inject `IDataFiles` + `DataPaths`, use `DataPaths.ResolveDownload()` and `IDataFiles` methods, remove `_paths` field and `ComputePaths()`
- [x] 7.3 Update `DownloadWorkerStateTests` and `DownloadPathsTests` → new `DataPathsTests` covers the path logic
- [x] 7.4 Delete `FfmpegRunnerTests.BuildArguments*` tests if they reference `DownloadPaths` (they don't — verify)

## 8. Migrate RuleSet Domain

- [x] 8.1 Update `RuleSetManager` — inject `IDataFiles` + `DataPaths`, replace `Directory.GetFiles()` with `_dataFiles.ListFiles()`, replace `FileSystemWatcher` creation with `_dataFiles.Watch()`, remove `_communityDir`/`_localDir` fields
- [x] 8.2 Update `RuleSetManagerState` — `CheckRuleSetPaths` uses `IDataFiles.Exists()` instead of `File.Exists()`
- [x] 8.3 Update `RuleSetWorker` — inject `IDataFiles`, replace `File.Exists()` + `File.ReadAllText()` with `_dataFiles.Exists()` + `_dataFiles.ReadText()`
- [x] 8.4 Update `RuleSetUpdater` — inject `IDataFiles` + `DataPaths`, replace `Directory.CreateDirectory()` / `Directory.Move()` / `Directory.Delete()` / `File.WriteAllTextAsync()` with `IDataFiles` methods, use `_dataFiles.ReplaceDirectory()` for atomic swap
- [x] 8.5 Update `RuleSetManagerTests` to use `MockFileSystem` via `IDataFiles`

## 9. Migrate Health/Setup

- [x] 9.1 Update `SetupApiEndpoints` — inject `DataPaths` + `IDataFiles`, use `_dataFiles.CanWrite()` instead of manual write-test, use `DataPaths` for directory paths
- [x] 9.2 Update `SetupHealthCheckTests` to use `MockFileSystem`

## 10. Cleanup and Verification

- [x] 10.1 Run `dotnet format` across the solution
- [x] 10.2 Run all tests — verify no direct `File.*`/`Directory.*` calls remain in domain code (except `DataFiles` implementation)
- [x] 10.3 Rebuild Docker container and verify dev setup still works (rulesets load, downloads work, health checks pass)
- [x] 10.4 Update existing main specs via `openspec` sync
