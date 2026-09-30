## Context

The download pipeline currently threads two configuration constants (`TempPath`, `DownloadPath`) through every message in the actor hierarchy. `IFileService` is a stateless path-builder — it computes paths but requires callers to supply the root directory each time. FFmpeg process management is copy-pasted across three workers with identical patterns for process lifecycle, timeout, and exit code handling. Tests duplicate `FakeFileService` implementations and create real temp directories on disk.

The pipeline flow today:

```
DownloadOptions (config)
  └─ QueueCoordinator (reads _tempPath, _downloadPath)
       └─ StartDownload(nzoId, videoUrl, subtitleUrl, tempPath, outputDir, title)
            └─ DownloadCoordinator (stores _tempPath, _outputDir, _videoPath, _subtitlePath)
                 ├─ FetchVideo(nzoId, url, tempPath) → Worker → VideoFetched(nzoId, path)
                 ├─ AcquireSubtitle(nzoId, ..., tempPath) → Worker → SubtitleAcquired(nzoId, path)
                 ├─ ConvertSubtitle(nzoId, path, tempPath) → Worker → SubtitleConverted(nzoId, path)
                 └─ RemuxVideo(nzoId, videoPath, subPath, outputDir, title) → Worker → VideoRemuxed(nzoId, path)
```

## Goals / Non-Goals

**Goals:**
- `IFileService` owns root paths and all file I/O — callers provide only identity (`nzoId`, `title`)
- `IFfmpegService` owns all FFmpeg/ffprobe process execution with centralized timeout and error handling
- Messages carry only semantic data (nzoId, URLs, title), not infrastructure paths
- `DownloadCoordinator` becomes a pure stage-machine without path state
- `FileService` backed by `IFileSystem` (System.IO.Abstractions) for in-memory testing
- Remove duplicated `FakeFileService` test implementations

**Non-Goals:**
- Replacing hand-rolled FFmpeg args with a library like FFMpegCore (the args are simple; the interface allows swapping later)
- Changing the actor hierarchy or worker-per-stage pattern (workers stay as actors for supervision/isolation)
- Adding download progress reporting (separate concern, exists in `download-service` spec)
- Modifying `SubtitleNormalizer` internals (it stays a static utility; `IFileService` calls it)

## Decisions

### Decision 1: IFileService owns roots via IOptions\<DownloadOptions\>

`FileService` receives `IOptions<DownloadOptions>` at construction and reads `TempPath`/`DownloadPath` once. All path methods drop their root-directory parameter.

**Alternative considered:** Inject paths as constructor parameters directly. Rejected because `IOptions<DownloadOptions>` is already the established pattern in this codebase and integrates with options validation.

```
Before: string GetTempVideoPath(string tempPath, string nzoId)
After:  string GetTempVideoPath(string nzoId)

Before: string GetOutputPath(string downloadPath, string title)
After:  string GetOutputPath(string title)
```

### Decision 2: IFileService gains managed I/O methods

New methods that handle the full write cycle — callers hand data, service handles path + I/O:

| Method | Replaces |
|--------|----------|
| `SaveVideoAsync(nzoId, Stream)` | Mp4DownloadWorker's manual FileStream creation |
| `SaveSubtitleAsync(nzoId, byte[], extension)` | SubtitleDownloadWorker's GetPath + WriteSubtitleAsync combo |
| `NormalizeSubtitleAsync(nzoId)` | SubtitleConvertWorker's Path.Combine + SubtitleNormalizer call |
| `CleanupTemp(nzoId)` | RemuxWorker's CleanupTempFiles with explicit paths |

Path-only methods (`GetTempVideoPath`, `GetSubtitlePath`, `GetOutputPath`) remain for cases where external tools (FFmpeg) need paths as arguments.

### Decision 3: System.IO.Abstractions as filesystem abstraction

`FileService` takes `IFileSystem` as a constructor dependency. Production DI registers `FileSystem` (the real implementation). Tests use `MockFileSystem`.

**Alternative considered:** Keep the current approach of real temp directories in tests. Rejected because it's slow, flaky (cleanup failures), and every worker test duplicates a `FakeFileService` with identical Path.Combine logic.

**Alternative considered:** No abstraction, just mock `IFileService` in tests. This works for worker tests (they mock the interface), but doesn't help test `FileService` itself without hitting disk.

Registration in `FunkArrServiceSetup`:
```csharp
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<IFileService, FileService>();
```

### Decision 4: New IFfmpegService with Process-based implementation

```csharp
public interface IFfmpegService
{
    Task DownloadHlsAsync(string nzoId, string url, CancellationToken ct = default);
    Task<bool> ExtractSubtitleAsync(string nzoId, string manifestUrl, CancellationToken ct = default);
    Task<string> RemuxAsync(string nzoId, string title, bool hasSubtitle, CancellationToken ct = default);
    Task<bool> HasSubtitleStreamAsync(string manifestUrl, CancellationToken ct = default);
}
```

`FfmpegService` takes `IFileService` as dependency and asks it for all paths. Contains the centralized `RunProcessAsync` helper that all three current workers duplicate:
- Process creation with redirected stderr
- CancellationToken → timeout + kill
- Exit code evaluation
- Logging

**Alternative considered:** Use FFMpegCore NuGet for fluent argument building. Rejected because our FFmpeg invocations are simple (3 distinct arg patterns), and the fluent API is harder to mock. The `IFfmpegService` interface allows swapping the implementation later if needed.

`BuildFfmpegArgs` methods move from workers into `FfmpegService` as `internal static` for continued unit testability.

### Decision 5: Messages lose path parameters, responses lose path fields

After:
```csharp
// Commands — identity + semantics only
record StartDownload(string NzoId, string VideoUrl, string? SubtitleUrl, string Title);
record FetchVideo(string NzoId, string Url);
record AcquireSubtitle(string NzoId, string? SubtitleUrl, string? HlsManifestUrl);
record ConvertSubtitle(string NzoId);
record RemuxVideo(string NzoId, string Title, bool HasSubtitle);

// Responses — identity only, no paths
record VideoFetched(string NzoId);
record SubtitleAcquired(string NzoId, bool Found);
record SubtitleConverted(string NzoId);
record VideoRemuxed(string NzoId);
```

Workers derive paths from `IFileService` using the `nzoId` they receive. The `DownloadCoordinator` no longer needs `_videoPath`/`_subtitlePath` state — it tracks `bool _hasSubtitle` instead.

### Decision 6: Persistence DTO backward compatibility

`DcJobAcceptedDto` keeps its `[JsonProperty("tmp")]` and `[JsonProperty("out")]` fields per the extend-only DTO rule. The domain event `JobAccepted` drops `TempPath`/`OutputDir`. The DTO mapping writes empty strings for new events. Recovery ignores the DTO path values — `DownloadCoordinator` gets paths from `IFileService` (which reads config).

This is actually more correct than today: if the user changes download paths between restarts, in-flight jobs use the new paths on recovery.

### Decision 7: Workers stay as actors

Workers remain as individual actors despite becoming thin. The actor pattern provides:
- **Supervision isolation**: A crashed FFmpeg process or I/O error stops the worker, not the coordinator
- **`Terminated` watching**: Coordinator detects unexpected worker death
- **Lifecycle management**: `Context.Stop(Self)` for self-cleanup

## Risks / Trade-offs

**[Risk] `SubtitleNormalizer` uses `File.Copy` and `File.ReadAllTextAsync` directly, not `IFileSystem`**
→ `NormalizeSubtitleAsync` on `IFileService` wraps the call and handles path resolution. `SubtitleNormalizer` itself remains a static text-processing utility. If testability of the normalizer's file I/O becomes a concern, it can be refactored to accept `IFileSystem` later — but its tests already work with real temp files and are fast.

**[Risk] Persistence events with empty path fields**
→ Old events replay with populated fields that are ignored. New events write empty strings. Both are valid per the DTO contract. If a rollback to old code occurs, recovered jobs would have empty paths and fail — but this is acceptable since the old code already has the stale-path-on-config-change bug.

**[Risk] `IFfmpegService` methods are coarse-grained**
→ `RemuxAsync` does EnsureOutputDirectory + build args + run process + cleanup temp. This is intentional — workers shouldn't know the steps. If a step needs to be customized per-caller in the future, the method can accept options.

**[Trade-off] Two new files (IFfmpegService.cs, FfmpegService.cs) for three use cases**
→ Worth it: eliminates ~60 lines of duplicated process boilerplate across three workers and centralizes timeout/error handling.

## Open Questions

None — all decisions were validated during the explore session.
