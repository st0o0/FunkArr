## Why

The FFmpeg integration is hand-rolled across 4 files (FfmpegProcess, FfmpegRunner, FfmpegArgumentBuilder, FfmpegProgressParser) with manual process management, manual argument quoting (fragile — breaks on special characters in URLs), and custom progress parsing. FFMpegCore is a mature, MIT-licensed NuGet package (~7.2M downloads) that handles all of this out of the box. Replacing the hand-rolled code with FFMpegCore reduces maintenance surface and eliminates the manual quoting/parsing bugs.

## What Changes

- Add FFMpegCore NuGet package dependency
- Delete FfmpegProcess.cs, FfmpegArgumentBuilder.cs, FfmpegProgressParser.cs
- Replace FfmpegRunner static class with an `IFfmpegRunner` interface + `FfmpegRunner` implementation backed by FFMpegCore
- The runner has no Akka dependency — returns `Task<FfmpegResult>`, reports progress via `Action<ProgressUpdate>` callback
- DownloadWorker injects `IFfmpegRunner` via DI and bridges to actor messaging (`PipeTo`)
- Remove dead code: `FfmpegArgumentBuilder.BuildWithoutSubtitle` (unused in production)
- Delete existing FFmpeg unit tests (FfmpegArgumentBuilderTests, FfmpegProgressParserTests) — they test the hand-rolled implementations being removed
- Add new tests for the FfmpegRunner wrapper to verify argument construction via FFMpegCore

## Capabilities

### New Capabilities

_None — this is a reimplementation of existing capability._

### Modified Capabilities

- `ffmpeg-process`: Requirements stay the same (same arguments, same progress data, same lifecycle), but the implementation shifts from hand-rolled process management to FFMpegCore. The spec scenarios for argument format may need adjustment if FFMpegCore produces equivalent but not character-identical arguments.

## Impact

- **FunkArr.Download**: FfmpegProcess.cs, FfmpegArgumentBuilder.cs, FfmpegProgressParser.cs deleted; FfmpegRunner.cs rewritten; DownloadWorker.cs updated for DI injection
- **FunkArr.Download.Tests**: FfmpegArgumentBuilderTests.cs, FfmpegProgressParserTests.cs deleted; new tests for runner wrapper
- **Dependencies**: FFMpegCore added to Directory.Packages.props
- **Docker**: No change — ffmpeg binary already installed in container image
- **DI registration**: IFfmpegRunner registered in service setup
