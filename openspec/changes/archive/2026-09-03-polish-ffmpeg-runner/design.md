## Context

The FfmpegRunner was introduced in `clean-ffmpeg-integration` using FFMpegCore. It works but has rough edges: public types leaked for DI, faked ExitCode, unbounded stderr, and tests that assert FFMpegCore's internal argument strings. This change polishes it to production quality.

## Goals / Non-Goals

**Goals:**
- All FFmpeg types internal to FunkArr.Download
- Real stderr from FFMpegException instead of manual collection
- Bounded error output
- Tests that cover our logic, not FFMpegCore's

**Non-Goals:**
- Changing download behavior or business logic
- Adding new FFmpeg features
- Changing the DownloadWorker persistence model

## Decisions

### Decision 1: DI via extension method

Add `DownloadServiceExtensions.AddFfmpegRunner(this IServiceCollection)` in FunkArr.Download. The host calls `services.AddFfmpegRunner()` — no `using FunkArr.Download` needed since extension methods are discovered by namespace.

All types become internal: `IFfmpegRunner`, `FfmpegRunner`, `FfmpegResult`, `ProgressUpdate`.

**Why not keep public?** These types are Download-domain internals. No other project consumes them. Public access was an artifact of the DI registration pattern, not a design intent.

### Decision 2: throwOnError: true + catch FFMpegException

FFMpegException has `FFMpegErrorOutput` (stderr string) but no typed `ExitCode`. The new error handling:

```csharp
try
{
    await processor.ProcessAsynchronously(throwOnError: true);
    return new FfmpegResult(true, 0, null, elapsed);
}
catch (FFMpegException ex)
{
    return new FfmpegResult(false, 1, CapStderr(ex.FFMpegErrorOutput), elapsed);
}
catch (OperationCanceledException)
{
    return new FfmpegResult(false, -1, "Cancelled", elapsed);
}
```

This eliminates:
- `NotifyOnError` callback
- `stderrLines` list
- `JoinStderr` method

ExitCode convention: `0` = success, `1` = FFmpeg failure (FFMpegException doesn't expose the real code), `-1` = cancelled. The DownloadWorker doesn't use ExitCode for branching logic — it checks `Success` and `Error` — so the exact value doesn't matter for behavior.

**Why not throwOnError: false?** With `false`, we get a `bool` return and must manually collect stderr via `NotifyOnError`. With `true`, we get stderr for free via the exception. Less code, fewer moving parts.

### Decision 3: Cap stderr via string truncation

Instead of counting lines during collection, we truncate `FFMpegErrorOutput` after the fact. Take the last ~4KB (roughly 50 lines). Simpler than a ring buffer since we get the full string from the exception anyway.

```csharp
private static string CapStderr(string? stderr) =>
    stderr is null or { Length: <= 4096 } ? stderr ?? "" : stderr[^4096..];
```

### Decision 4: BuildArguments becomes private, argument tests deleted

`BuildArguments` was `internal static` solely for test access. The 3 argument tests (`_video_only_`, `_with_subtitle_`, `_hls_url_`) assert on `processor.Arguments` as a string — testing FFMpegCore's rendering, not our logic. Delete them.

`ParseProgressLine` stays `internal static` — the 4 progress tests exercise our parsing logic and are valuable.

## Risks / Trade-offs

- **[No real ExitCode]** → FFMpegException doesn't expose it. Convention (0/1/-1) is sufficient since the worker branches on `Success`, not on the code. If a future need arises, we'd need to fork or contribute to FFMpegCore.
- **[Stderr truncation loses context]** → We keep the tail (last 4KB) which usually contains the actual error. If we need full stderr, it's in FFmpeg's own logs (when run with `-loglevel`).
