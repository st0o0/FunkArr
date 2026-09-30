## Context

The Download domain currently manages FFmpeg through 4 hand-rolled files: `FfmpegProcess` (System.Diagnostics.Process wrapper), `FfmpegRunner` (static orchestrator with Akka coupling), `FfmpegArgumentBuilder` (manual string quoting), and `FfmpegProgressParser` (key=value parser). This works but has fragile quoting, mixes Akka concerns into infrastructure code, and duplicates what FFMpegCore provides out of the box.

FFMpegCore (MIT, ~7.2M downloads) is the target replacement. It provides fluent argument building, built-in progress parsing, process lifecycle management, and proper stderr capture.

## Goals / Non-Goals

**Goals:**
- Replace all hand-rolled FFmpeg code with FFMpegCore
- Clean separation: `IFfmpegRunner` interface with no Akka dependency
- DownloadWorker injects the runner via DI
- Progress reporting via `Action<ProgressUpdate>` callback
- Cancellation via CancellationToken
- Testable — worker can be tested with a mock runner

**Non-Goals:**
- Changing download business logic (retry-without-subtitles stays in worker)
- Adding new FFmpeg features (audio normalization, re-encoding, etc.)
- Changing the DownloadWorker persistence model
- Probing/analyzing media before download

## Decisions

### Decision 1: IFfmpegRunner as DI-injectable service

The runner is a non-static class implementing `IFfmpegRunner`, registered as singleton in `ServiceSetupContainer`. The interface returns `Task<FfmpegResult>` — no Akka types cross the boundary.

```
interface IFfmpegRunner
    RunAsync(videoUrl, subtitleUrl?, outputPath, onProgress, ct) → Task<FfmpegResult>

record FfmpegResult(bool Success, int ExitCode, string? Error, int ElapsedSeconds)
record ProgressUpdate(long TotalSize, long OutTimeUs, double Speed)
```

**Why not static?** Static prevents DI injection and mocking. The previous `FfmpegRunner.Run(IActorRef self, ...)` leaked Akka into infrastructure.

**Why singleton?** The runner is stateless — it builds and runs FFmpeg processes. No per-request state.

### Decision 2: FFMpegCore fluent API with WithCustomArgument for subtitles

FFMpegCore has no built-in subtitle codec or metadata methods. The subtitle case uses `WithCustomArgument`:

```csharp
// Video-only:
FFMpegArguments.FromUrlInput(uri)
    .OutputToFile(path, overwrite: true, o => o.CopyChannel())

// Video + subtitle:
FFMpegArguments.FromUrlInput(videoUri)
    .AddUrlInput(subtitleUri)
    .OutputToFile(path, overwrite: true, o => o
        .WithCustomArgument("-c:v copy -c:a copy -c:s srt")
        .WithCustomArgument("-metadata:s:s:0 language=deu"))
```

**Alternative considered:** Raw argument string via `FFMpegArguments.FromPipeInput` — rejected because it bypasses FFMpegCore's process management entirely, losing the benefit of the library.

### Decision 3: CancellationToken bridging

FFMpegCore's `ProcessAsynchronously()` does not accept a `CancellationToken` directly. It uses an internal cancellation mechanism. We bridge this by registering a callback on our token that triggers the FFMpegCore cancellation:

```csharp
var processor = FFMpegArguments...;
using var reg = ct.Register(() => processor.Cancel());  // or Kill if Cancel not available
await processor.ProcessAsynchronously();
```

If FFMpegCore doesn't expose a clean cancel surface, we fall back to wrapping the task and killing the process via `Process.Kill()` on token cancellation — same behavior as the current hand-rolled code.

### Decision 4: Progress via NotifyOnProgress + callback bridge

FFMpegCore has built-in progress parsing via `NotifyOnProgress(Action<TimeSpan>)`. However, our `ProgressUpdate` record needs `TotalSize`, `OutTimeUs`, and `Speed` — FFMpegCore's time-based callback only provides elapsed time.

Two options:
1. **Use FFMpegCore's NotifyOnOutput** to get raw `-progress pipe:1` lines and parse them ourselves (keeps our existing ProgressUpdate shape)
2. **Use FFMpegCore's time-based progress** and drop TotalSize/Speed from ProgressUpdate

We go with option 1 — `NotifyOnOutput` gives us the raw progress lines, and we keep a thin parser for the 3 fields we need. This preserves the progress data the UI already consumes. The parser is much simpler than the current one since FFMpegCore handles the process I/O.

### Decision 5: Keep ProgressUpdate and FfmpegResult as internal records in FunkArr.Download

These types are internal to the Download domain. `ProgressUpdate` is the same record that exists today — the worker already handles it. `FfmpegResult` replaces `ProcessExited`.

### Decision 6: DI registration in ServiceSetupContainer

`services.AddSingleton<IFfmpegRunner, FfmpegRunner>()` in `ServiceSetupContainer.SetupServices()`. No FFMpegCore global configuration needed — it finds `ffmpeg` on PATH by default, which matches our Docker setup.

## Risks / Trade-offs

- **[FFMpegCore version churn]** → Pinned in Directory.Packages.props. Low risk — the library is mature and the API we use (fluent arguments, process execution) is stable.
- **[WithCustomArgument fragility]** → Subtitle args use string-based custom arguments instead of typed methods. If FFMpegCore changes argument ordering, our custom args could conflict. → Mitigation: integration test that verifies the actual FFmpeg command line.
- **[CancellationToken bridge]** → If FFMpegCore's internal cancellation doesn't expose a clean API, we may need to access the underlying Process. → Mitigation: FFMpegCore is open source, we can check the actual mechanism and adapt.
- **[Progress data shape]** → If we need raw progress lines via NotifyOnOutput and FFMpegCore changes how it handles stdout, our parser breaks. → Mitigation: thin parser, easy to fix. Same risk as today with our own process management.
