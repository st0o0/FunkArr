## Context

FunkArr.Download uses FFMpegCore 5.5.0 to spawn FFmpeg processes for video remuxing. The current `FfmpegProcess` class builds arguments via FFMpegCore's fluent API but relies heavily on `.WithCustomArgument()` string escapes for subtitle handling, BSF tolerance, progress pipe, user-agent, and proxy settings. It also hand-rolls progress line parsing, error extraction, and failure classification — all of which are duplicated from the project owner's FFmpeg.Rx library.

The architecture is well-isolated: `DownloadWorker` → `IRemuxer` (Remuxer) → `IFfmpegProcess` (FfmpegProcess). Only `FfmpegProcess` touches FFMpegCore directly. The `IFfmpegProcess` interface is the replacement boundary.

## Goals / Non-Goals

**Goals:**
- Replace FFMpegCore with FFmpeg.Rx as the FFmpeg process execution layer
- Eliminate duplicated progress parsing, error classification, and error extraction code
- Use FFmpeg.Rx's typed builders (SubtitleCodec, StreamBuilder, Disposition, Metadata) where they replace string-based custom arguments
- Keep the `IFfmpegProcess` interface contract unchanged from Remuxer's perspective
- Maintain all existing behavioral guarantees (stream mapping, BSF tolerance, subtitle handling, progress, cancellation)

**Non-Goals:**
- Changing the Remuxer, DownloadWorker, or any code above the IFfmpegProcess boundary
- Using FFmpeg.Rx's reactive Observable streams for progress (will subscribe and bridge to the existing `Action<ProgressUpdate>` callback)
- Adding new FFmpeg.Rx features (probing, encoding, filter graphs) — this is a like-for-like swap
- Renaming FunkArr's own `FfmpegResult` or `ProgressUpdate` types — they're part of the public API

## Decisions

### Decision 1: Rewrite FfmpegProcess internals, keep IFfmpegProcess interface

**Choice**: Rewrite `FfmpegProcess.ExecuteAsync` to use `Ffmpeg.Input(uri).WithProgressReporting().Output(path, ...)` → `session.RunAsync(ct)`, then map the result.

**Why**: The `IFfmpegProcess` boundary means nothing above it needs to change. The interface signature stays `Task<FfmpegResult> ExecuteAsync(FfmpegInput input, Action<ProgressUpdate> onProgress, CancellationToken ct)`.

### Decision 2: Bridge Observable progress to Action callback via Subscribe

**Choice**: Subscribe to `session.Progress` with a delegate that maps `ProgressEvent` → `ProgressUpdate` and invokes `onProgress`.

**Why not expose Observable up the stack**: That would require changing IFfmpegProcess, IRemuxer, and DownloadWorker's PipeTo pattern. Unnecessary churn for this migration. The `Action<ProgressUpdate>` callback works fine for the actor's needs.

**Alternative considered**: Ignore the Observable and re-parse progress lines ourselves. Rejected — defeats the purpose of the migration.

### Decision 3: Map enum and result types at the FfmpegProcess boundary

**Choice**: FfmpegProcess maps between FFmpeg.Rx types and FunkArr types:
- `FFmpeg.Rx.Events.ProgressEvent` → `FunkArr.Messages.Download.ProgressUpdate` (field rename: BytesWritten→TotalSize, TimeUs→OutTimeUs)
- `FFmpeg.Rx.Models.FfmpegResult` → `FunkArr.Download.Ffmpeg.FfmpegResult` (Duration→ElapsedSeconds, FailureKind mapping)
- `FFmpeg.Rx.FailureKind` → `FunkArr.Messages.Download.FailureKind` (exhaustive switch, Unknown maps to Permanent)

**Why**: FunkArr's types are part of the public Messages/Persistence contract. Changing their field names or enum values would be a breaking change across the codebase. The mapping is trivial (3-4 lines).

### Decision 4: Use typed builders where available, CustomArg for the rest

**Choice**:
- **Typed**: `.Stream(0, 0, StreamType.Video, s => s.Copy())`, `.Stream(1, StreamType.Subtitle, s => s.Encode(SubtitleCodec.Srt).Disposition("0").Metadata("language", lang))`, `.WithProgressReporting()`
- **CustomArg**: `-bsf:a aac_adtstoasc=no_validation=1` (no typed BSF builder), `-user_agent "Mozilla/5.0"` (no typed input option), `-http_proxy <url>` (no typed input option)

**Why**: Use typed API where it adds safety, fall back to CustomArg for niche FFmpeg flags that don't have typed equivalents. The escape hatch exists for exactly this purpose.

### Decision 5: Input options via InputOptionConfig CustomArgs

**Choice**: Pass user-agent and proxy as `InputOptionConfig(CustomArgs: [...])` on the `UrlInputConfig`.

**Why**: FFmpeg.Rx's `InputOptionConfig` has typed fields for HWAccel, Seek, Framerate, etc. but not for HTTP options like user-agent and proxy. CustomArgs is the correct extension point, and the arguments are placed before `-i` by the serializer (verified in ArgumentSerializer).

### Decision 6: Dispose IFfmpegSession after RunAsync

**Choice**: Use `await using var session = ...` pattern to ensure the R3 Subject is disposed after execution.

**Why**: IFfmpegSession implements IAsyncDisposable. The Subject must be disposed to avoid memory leaks.

## Risks / Trade-offs

- **[FFmpeg.Rx is 0.1.0]** → The package is pre-1.0 and owned by the same developer. Breaking changes in FFmpeg.Rx are controlled by the user. Risk is acceptable for a same-author dependency.
- **[R3 transitive dependency]** → FFmpeg.Rx brings R3 into FunkArr.Download's dependency tree. R3 is not used elsewhere in FunkArr. → Acceptable; R3 is lightweight and the Observable subscription is contained within FfmpegProcess. No R3 types leak through IFfmpegProcess.
- **[CliWrap transitive dependency]** → FFmpeg.Rx uses CliWrap instead of System.Diagnostics.Process (which FFMpegCore uses internally). → No behavioral difference for FunkArr; CliWrap is well-tested and handles process lifecycle correctly.
- **[Test coverage shift]** → ParseProgressLine, ExtractError, ClassifyFailure tests move from FunkArr.Download.Tests to FFmpeg.Rx.Tests. FunkArr's test file shrinks. → The code is tested in the library; FunkArr tests now focus on the builder output and integration behavior.
