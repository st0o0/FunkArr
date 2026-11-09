# Design: FFmpeg.Rx Fluent API Migration

## FfmpegProcess Changes

### Method signature

`BuildSession(FfmpegInput)` stays `internal static`, returns `IFfmpegSession`.
`BuildPipeline`, `BuildInputs`, `BuildOutput` are removed.

### Implementation

```
BuildSession(FfmpegInput input):

  1. Video input with typed HTTP options
     Ffmpeg.Input(new Uri(input.VideoUrl), opts =>
     {
         opts.UserAgent(UserAgent);
         if (input.ProxyUrl is not null)
             opts.HttpProxy(input.ProxyUrl);
     })

  2. Progress reporting
     .WithProgressReporting()

  3. Conditional subtitle input
     if (input.SubtitlePath is not null)
         builder.Input(input.SubtitlePath)

  4. Output with stream mapping
     .Output(input.OutputPath, output =>
     {
         output
             .Stream(0, 0, Video, s => s.Copy())
             .Stream(0, 0, Audio, s =>
             {
                 s.Copy();
                 if (input.IsHls)
                     s.BitstreamFilter("aac_adtstoasc=no_validation=1");
             });

         if (input.SubtitlePath is not null)
             output.Stream(1, Subtitle, s => s
                 .Encode(SubtitleCodec.Srt)
                 .Metadata("language", input.SubtitleLanguage ?? "deu")
                 .Disposition("0"));
     })
```

### Key differences from current code

| Aspect | Before (0.1.0) | After (0.1.1) |
|--------|----------------|---------------|
| User-Agent | `CustomArgs: ["-user_agent \"...\""]` | `opts.UserAgent("...")` |
| HTTP Proxy | `CustomArgs: ["-http_proxy ..."]` | `opts.HttpProxy("...")` |
| BSF filter | `OutputConfig.CustomArgs: ["-bsf:a ..."]` | `s.BitstreamFilter("...")` |
| Progress | `GlobalOptions: ["-progress pipe:1"]` | `.WithProgressReporting()` |
| Session | `FfmpegSession.Create(config)` | `Output()` returns session |
| Stream mapping | `new StreamConfig(0, 0, ...)` constructor | `output.Stream(0, 0, ..., s => ...)` |

## Test Changes

### Removed tests (assert on removed internals)

- `BuildInputs_includes_user_agent` — was checking CustomArgs strings
- `BuildInputs_with_proxy_includes_http_proxy` — same
- `BuildInputs_without_proxy_omits_http_proxy` — same
- `BuildInputs_with_subtitle_adds_file_input` — internal structure
- `BuildPipeline_includes_progress_global_option` — GlobalOptions string
- `BuildPipeline_output_destination_is_file` — internal record structure
- All `BuildPipeline_*` and `BuildOutput_*` that assert on OutputConfig internals

### Replacement tests (assert on session creation)

Each scenario verifies `BuildSession` returns a non-null `IFfmpegSession`:

- `BuildSession_direct_mp4_creates_session` — basic video-only
- `BuildSession_hls_creates_session` — HLS input
- `BuildSession_with_subtitle_creates_session` — two-input session
- `BuildSession_with_proxy_creates_session` — proxy option set
- `BuildSession_hls_with_subtitle_creates_session` — combined scenario
- `BuildSession_progress_observable_available` — `session.Progress` is not null

### Kept tests (don't touch FfmpegProcess internals)

- All `RemuxOptions` tests (Create, WithSubtitle, WithChannel, fluent chaining)
- These test FunkArr's own `RemuxOptions` record, unrelated to FFmpeg.Rx

## Files Changed

| File | Change |
|---|---|
| `Directory.Packages.props` | FFmpeg.Rx 0.1.0 → 0.1.1 |
| `FunkArr.Download/Ffmpeg/FfmpegProcess.cs` | Replace 3 builder methods with single fluent `BuildSession` |
| `FunkArr.Download.Tests/Ffmpeg/RemuxerTests.cs` | Replace config-assertion tests with session-creation tests |
