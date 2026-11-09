# Tasks

## Package update

- [x] Bump FFmpeg.Rx version from 0.1.0 to 0.1.1 in `src/Directory.Packages.props`

## FfmpegProcess rewrite

- [x] Replace `BuildPipeline`, `BuildInputs`, `BuildOutput` with single fluent `BuildSession` method using `Ffmpeg.Input().WithProgressReporting().Output()`
- [x] Use `InputOptionsBuilder.UserAgent()` and `.HttpProxy()` instead of CustomArgs
- [x] Use `StreamBuilder.BitstreamFilter()` for HLS aac_adtstoasc instead of output CustomArgs
- [x] Use `OutputBuilder.Stream(1, Subtitle, ...)` for subtitle stream (no stream index)
- [x] Remove all `CustomArgs` usage from FfmpegProcess

## Test rewrite

- [x] Remove tests that assert on `BuildPipeline`/`BuildInputs`/`BuildOutput` return values
- [x] Add `BuildSession_direct_mp4_creates_session` — basic video-only
- [x] Add `BuildSession_hls_creates_session` — HLS with BSF
- [x] Add `BuildSession_with_subtitle_creates_session` — two inputs
- [x] Add `BuildSession_with_proxy_creates_session` — proxy option
- [x] Add `BuildSession_hls_with_subtitle_creates_session` — combined
- [x] Add `BuildSession_progress_observable_available` — Progress not null
- [x] Keep all `RemuxOptions_*` tests unchanged

## Validation

- [x] Run `dotnet build src/FunkArr.slnx`
- [x] Run `dotnet run --project src/FunkArr.Download.Tests`
- [x] Run `dotnet format src/FunkArr.slnx --verify-no-changes` (changed files only)
