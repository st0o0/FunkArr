## Why

`IFileService` is a stateless path-builder that requires every caller to pass root directories (`tempPath`, `downloadPath`) on every call. These roots are configuration constants that get threaded as raw strings through every message in the download pipeline — from `QueueCoordinator` through `StartDownload`, `DownloadCoordinator` state, and into every worker command (`FetchVideo`, `AcquireSubtitle`, `ConvertSubtitle`, `RemuxVideo`). They are even persisted in event-sourced DTOs, meaning a config change between restarts replays stale paths. Meanwhile, FFmpeg process management is duplicated across three workers (`HlsDownloadWorker`, `SubtitleExtractWorker`, `RemuxWorker`) with identical boilerplate for process lifecycle, timeout handling, and exit code evaluation.

## What Changes

- **IFileService redesign**: Inject root paths (`TempPath`, `DownloadPath`) via `IOptions<DownloadOptions>` at construction. Callers provide only identity (`nzoId`, `title`). Add managed I/O methods (`SaveVideoAsync`, `SaveSubtitleAsync`, `NormalizeSubtitleAsync`) so callers hand data to the service instead of doing file I/O themselves.
- **System.IO.Abstractions foundation**: `FileService` implementation uses `IFileSystem` from the `System.IO.Abstractions` NuGet package, enabling tests with `MockFileSystem` instead of real temp directories.
- **New IFfmpegService**: Extracts all FFmpeg/ffprobe process execution into a dedicated service. Uses `IFileService` for path resolution. Encapsulates process lifecycle, argument building, timeout handling, and exit code evaluation.
- **Message simplification**: `StartDownload`, `FetchVideo`, `AcquireSubtitle`, `ConvertSubtitle`, `RemuxVideo` drop `TempPath`/`OutputDir` parameters. Response messages (`VideoFetched`, `SubtitleAcquired`, `SubtitleConverted`) drop path fields — paths are deterministic from `nzoId` via `IFileService`.
- **DownloadCoordinator state reduction**: Drops `_tempPath`, `_outputDir`, `_videoPath`, `_subtitlePath` fields. Becomes a pure stage-machine without path tracking.
- **Persistence DTO compatibility**: `DcJobAcceptedDto` retains `[JsonProperty("tmp")]` and `[JsonProperty("out")]` fields (extend-only rule) but recovery ignores them — paths come from config via `IFileService`.
- **Test cleanup**: Remove duplicated `FakeFileService` implementations from worker tests. `FileService` tests use `MockFileSystem`.

## Capabilities

### New Capabilities
- `ffmpeg-service`: Centralized FFmpeg/ffprobe process execution service — HLS download, subtitle extraction, subtitle stream probing, and video/subtitle remuxing to MKV.

### Modified Capabilities
- `file-operations`: Path methods lose root-directory parameters; roots injected at construction. New managed I/O methods added (`SaveVideoAsync`, `SaveSubtitleAsync`, `NormalizeSubtitleAsync`, `CleanupTemp`). Implementation backed by `IFileSystem` from `System.IO.Abstractions`.
- `download-coordinator`: Drops `_tempPath`, `_outputDir`, `_videoPath`, `_subtitlePath` state. `StartDownload` message loses `TempPath`/`OutputDir`. Recovery ignores persisted path fields.
- `download-pipeline`: Worker command messages (`FetchVideo`, `AcquireSubtitle`, `ConvertSubtitle`, `RemuxVideo`) lose path parameters. Response messages lose path fields. Workers delegate to `IFileService`/`IFfmpegService`.
- `queue-coordinator`: `TryStartNext` builds `StartDownload` without `_tempPath`/`_downloadPath`.

## Impact

- **Dependencies**: Add `System.IO.Abstractions` + `System.IO.Abstractions.TestingHelpers` (test project) NuGet packages.
- **DI registrations**: `FunkArrServiceSetup` registers `IFileSystem` (singleton), updated `IFileService`, new `IFfmpegService`.
- **Persistence**: No journal migration needed — old DTOs with path fields deserialize fine, values are just ignored on recovery. New events written without path data.
- **All DownloadClient workers**: Thinned to orchestration shells delegating to services.
- **All DownloadClient worker tests**: Rewritten to mock `IFileService`/`IFfmpegService` instead of duplicating `FakeFileService`.
