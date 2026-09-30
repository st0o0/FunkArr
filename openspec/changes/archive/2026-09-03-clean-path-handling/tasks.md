## 1. DownloadPaths Value Object

- [x] 1.1 Create `DownloadPaths` sealed record in `FunkArr.Download` with `IncompletePath`, `CompletePath`, `RelativePath` properties and static `Compute()` factory method containing all path/naming logic (category resolution, episode identifier regex, directory disambiguation)
- [x] 1.2 Write unit tests for `DownloadPaths.Compute()` covering: episode identifier (S01E05, .E05., date), no identifier (disambiguator), category resolution (known, unknown, null, custom Dir), case-insensitive matching

## 2. IDownloadFileOperations

- [x] 2.1 Create `IDownloadFileOperations` interface with `EnsureDirectory`, `MoveFile`, `DeleteDirectory` methods and `DownloadFileOperations` sealed implementation class
- [x] 2.2 Update `DownloadServiceExtensions` to register `IDownloadFileOperations` instead of `IDownloadFileService`, add `IHostedLifecycleService` registration for startup directory bootstrapping
- [x] 2.3 Delete `IDownloadFileService.cs` and `DownloadFileService.cs`

## 3. Persistence Events

- [x] 3.1 Remove `FilePath` from `DownloadSucceeded` persistence DTO (keep DownloadId, DownloadTimeSeconds, CompletedAt)
- [x] 3.2 Rename `FilePath` to `RelativePath` in `HistoryRecorded` persistence DTO

## 4. Messages

- [x] 4.1 Remove `FilePath` from `WorkerStatusResult` message
- [x] 4.2 Rename `FilePath` to `RelativePath` in `RecordDownload` message
- [x] 4.3 Rename `FilePath` to `RelativePath` in `HistoryItem` message

## 5. DownloadOptions

- [x] 5.1 Remove `ResolveCategoryDir` method from `DownloadOptions` in `FunkArr.Core`
- [x] 5.2 Update or remove `DownloadOptionsTests` that test `ResolveCategoryDir`

## 6. DownloadWorker

- [x] 6.1 Replace `_tempOutputPath`/`_outputPath` string fields with single `DownloadPaths?` field, update constructor to inject `IDownloadFileOperations` and `IOptions<DownloadOptions>` instead of `IDownloadFileService`
- [x] 6.2 Refactor `ComputePaths()` to use `DownloadPaths.Compute()`, update `HandleStart` to call `EnsureDirectory` on incomplete dir
- [x] 6.3 Refactor `HandleFfmpegResult` to use `IDownloadFileOperations.EnsureDirectory` + `MoveFile` + `DeleteDirectory`, pass `RelativePath` to `RecordDownload`, drop `FilePath` from `DownloadSucceeded`
- [x] 6.4 Remove `FilePath` from `HandleQueryStatus` response construction
- [x] 6.5 Update `OnRecoveryCompleted` to use `DownloadPaths.Compute()`

## 7. Download History

- [x] 7.1 Update `HistoryRecord` to use `RelativePath` instead of `FilePath`
- [x] 7.2 Update `DownloadHistoryManagerState.Apply(HistoryRecorded)` and `ToHistoryResult` to use `RelativePath`

## 8. SABnzbd Download API

- [x] 8.1 Update history endpoint in `DownloadApiEndpoints` to resolve `RelativePath` against `DownloadOptions.CompletePath` for the `storage` field
- [x] 8.2 Remove `FilePath` references from queue endpoint mapping

## 9. Cleanup & Verify

- [x] 9.1 Delete `DownloadFileServiceTests.cs`, update or create `DownloadFileOperationsTests` if needed
- [x] 9.2 Run `dotnet build FunkArr.slnx` and fix any remaining compilation errors
- [x] 9.3 Run `dotnet format` and fix any style violations
- [x] 9.4 Run all download domain tests (`dotnet run --project FunkArr.Download.Tests/FunkArr.Download.Tests.csproj`)
