## 1. Dependencies and Infrastructure

- [x] 1.1 Add `System.IO.Abstractions` to `Directory.Packages.props` and `FunkArr.csproj`; add `System.IO.Abstractions.TestingHelpers` to `FunkArr.Tests.csproj`
- [x] 1.2 Register `IFileSystem` as singleton (`FileSystem`) in `FunkArrServiceSetup`

## 2. IFileService Redesign

- [x] 2.1 Redesign `IFileService` interface: inject roots via `IOptions<DownloadOptions>`, remove root-directory parameters from path methods, add `SaveVideoAsync(nzoId, Stream)`, `SaveSubtitleAsync(nzoId, byte[], extension)`, `NormalizeSubtitleAsync(nzoId)`, `CleanupTemp(nzoId)`, `EnsureOutputDirectory(title)` — parameterless `EnsureDirectoriesExist()`
- [x] 2.2 Rewrite `FileService` implementation: constructor takes `IFileSystem` + `IOptions<DownloadOptions>`, all file operations use `IFileSystem`
- [x] 2.3 Rewrite `FileServiceTests` to use `MockFileSystem` instead of real temp directories
- [x] 2.4 Update `FunkArrServiceSetup` DI registration for new `FileService` constructor

## 3. IFfmpegService

- [x] 3.1 Create `IFfmpegService` interface in `DownloadClient/` with `DownloadHlsAsync`, `ExtractSubtitleAsync`, `HasSubtitleStreamAsync`, `RemuxAsync`
- [x] 3.2 Create `FfmpegService` implementation: inject `IFileService`, centralized `RunProcessAsync` helper, move `BuildFfmpegArgs` methods from workers as `internal static`
- [x] 3.3 Write `FfmpegServiceTests` for argument building methods (unit tests on `internal static` methods, no process execution)
- [x] 3.4 Register `IFfmpegService` as singleton in `FunkArrServiceSetup`

## 4. Message Simplification

- [x] 4.1 Update `DownloadCoordinatorMessages.cs`: `StartDownload` drops `TempPath`/`OutputDir`; `FetchVideo` drops `TempPath`; `AcquireSubtitle` drops `TempPath`; `ConvertSubtitle` drops `SubtitlePath`/`TempPath`; `RemuxVideo` drops `VideoPath`/`SubtitlePath`/`OutputDir`, adds `HasSubtitle`
- [x] 4.2 Update response messages: `VideoFetched` drops `VideoPath`; `SubtitleAcquired` replaces `SubtitlePath` with `bool Found`; `SubtitleConverted` drops `NormalizedPath`; `VideoRemuxed` drops `OutputPath`

## 5. Persistence DTO Compatibility

- [x] 5.1 Update `DownloadCoordinatorStageEvents.JobAccepted` domain event: remove `TempPath`/`OutputDir`
- [x] 5.2 Update `DownloadCoordinatorEventDtoMapping`: `ToDto` writes empty strings for `TempPath`/`OutputDir`; `ToDomain` ignores DTO path fields

## 6. DownloadCoordinator Refactor

- [x] 6.1 Remove `_tempPath`, `_outputDir`, `_videoPath`, `_subtitlePath` fields from `DownloadCoordinator`; add `bool _hasSubtitle`
- [x] 6.2 Update `ApplyAccepted` to not set path fields; update `HandleVideoFetched` / `HandleSubtitleAcquired` / `HandleSubtitleConverted` to work with boolean flag instead of path strings
- [x] 6.3 Update `HandleVideoRemuxed` to resolve output path via `IFileService.GetOutputPath(title)` for the completion notification
- [x] 6.4 Update worker spawn methods (`SpawnVideoWorker`, `SpawnSubtitleWorker`, `SpawnSubtitleConvertWorker`, `SpawnRemuxWorker`) to pass simplified messages
- [x] 6.5 Update or create `DownloadCoordinatorTests` to verify stage machine with new messages

## 7. Worker Refactoring

- [x] 7.1 Refactor `Mp4DownloadWorker`: inject `IFileService`, delegate to `SaveVideoAsync(nzoId, httpStream)`, tell `VideoFetched(nzoId)`
- [x] 7.2 Refactor `HlsDownloadWorker`: inject `IFfmpegService` instead of `IFileService`, delegate to `DownloadHlsAsync(nzoId, url)`, tell `VideoFetched(nzoId)`
- [x] 7.3 Refactor `SubtitleDownloadWorker`: delegate to `IFileService.SaveSubtitleAsync(nzoId, bytes, ext)`, tell `SubtitleAcquired(nzoId, true)`
- [x] 7.4 Refactor `SubtitleExtractWorker`: inject `IFfmpegService`, delegate to `ExtractSubtitleAsync(nzoId, manifestUrl)`, tell `SubtitleAcquired(nzoId, found)`
- [x] 7.5 Refactor `SubtitleConvertWorker`: inject `IFileService`, delegate to `NormalizeSubtitleAsync(nzoId)`, tell `SubtitleConverted(nzoId)`
- [x] 7.6 Refactor `RemuxWorker`: inject `IFfmpegService`, delegate to `RemuxAsync(nzoId, title, hasSubtitle)`, tell `VideoRemuxed(nzoId)`

## 8. QueueCoordinator Update

- [x] 8.1 Remove `_tempPath` and `_downloadPath` fields from `QueueCoordinator`; update constructor to only read `ConcurrentDownloads` from options
- [x] 8.2 Update `TryStartNext()` to build `StartDownload(nzoId, downloadUrl, subtitleUrl, title)` without path arguments

## 9. Worker Tests

- [x] 9.1 Rewrite `DirectDownloadWorkerTests` (Mp4DownloadWorker): mock `IFileService`, remove `FakeFileService`, verify `SaveVideoAsync` called
- [x] 9.2 Rewrite `HlsDownloadWorkerTests`: mock `IFfmpegService`, verify `DownloadHlsAsync` called
- [x] 9.3 Rewrite `SubtitleExtractWorkerTests`: mock `IFfmpegService`, remove `FakeFileService`
- [x] 9.4 Rewrite `SubtitleConvertWorkerTests`: mock `IFileService`, verify `NormalizeSubtitleAsync` called
- [x] 9.5 Rewrite `RemuxWorkerTests`: mock `IFfmpegService`, remove `FakeFileService`
- [x] 9.6 Update `DownloadRequestTrackerTests` if affected by message changes

## 10. Verification

- [x] 10.1 Run `dotnet build FunkArr.slnx` — verify zero errors
- [x] 10.2 Run full test suite — verify all tests pass (459 tests, 0 failures)
- [x] 10.3 Run `dotnet format` on all changed files
