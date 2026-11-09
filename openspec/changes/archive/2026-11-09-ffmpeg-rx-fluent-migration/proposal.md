# Migrate FfmpegProcess to FFmpeg.Rx 0.1.1 Fluent API

## Problem

`FfmpegProcess` manually constructs `PipelineConfig`, `InputConfig[]`, and `OutputConfig`
records across three `internal static` builder methods (~60 lines). HTTP options
(`-user_agent`, `-http_proxy`) and bitstream filters (`-bsf:a`) are passed as raw
`CustomArgs` strings — no type safety, easy to get escaping wrong.

## What Changed in FFmpeg.Rx 0.1.1

- `InputOptionsBuilder` with typed `UserAgent(string)`, `HttpProxy(string)`
- `PipelineBuilder.Output()` returns `IFfmpegSession` directly (no manual `FfmpegSession.Create`)
- `StreamBuilder.BitstreamFilter(string)` replaces output-level `CustomArgs`
- `PipelineBuilder.WithProgressReporting()` replaces `GlobalOptions: ["-progress pipe:1"]`
- `OutputBuilder.Stream()` with `StreamBuilder` fluent chain (Copy, Encode, Metadata, Disposition)

## Solution

Replace the three manual builder methods (`BuildPipeline`, `BuildInputs`, `BuildOutput`)
with a single `BuildSession` method that uses the FFmpeg.Rx fluent API end-to-end.

### Before (0.1.0 — record construction)

```
BuildSession → BuildPipeline → BuildInputs (CustomArgs)
                             → BuildOutput  (CustomArgs, manual StreamConfig)
                             → PipelineConfig(inputs, outputs, globalOptions)
            → FfmpegSession.Create(spec)
```

### After (0.1.1 — fluent API)

```
BuildSession → Ffmpeg.Input(url, opts => UserAgent/HttpProxy)
                     .Input(subtitlePath)       // conditional
                     .WithProgressReporting()
                     .Output(path, output =>
                         .Stream(Video, Copy)
                         .Stream(Audio, Copy + BitstreamFilter)
                         .Stream(Subtitle, Encode + Metadata))  // conditional
            → IFfmpegSession (returned directly)
```

## Test Strategy

The current tests assert on intermediate records (`PipelineConfig`, `InputConfig[]`,
`OutputConfig`). With full fluent, these intermediates don't exist — `Output()` returns
an opaque `IFfmpegSession`.

New approach: tests verify that input combinations produce a valid session without
error. Detailed FFmpeg argument correctness is FFmpeg.Rx's own testing responsibility.
FunkArr tests cover:
- Session creation succeeds for each scenario (MP4, HLS, subtitle, proxy, combined)
- The session has a `Progress` observable (progress reporting configured)
- Error cases (null URL etc.) are caught

## Scope

- `FfmpegProcess.cs` — rewrite builders to fluent API
- `RemuxerTests.cs` — adapt tests to session-based assertions
- `Directory.Packages.props` — bump FFmpeg.Rx 0.1.0 → 0.1.1
- No changes to `Remuxer.cs`, `IFfmpegProcess`, `FfmpegInput`, or messages
