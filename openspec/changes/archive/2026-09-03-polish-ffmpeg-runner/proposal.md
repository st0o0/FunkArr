## Why

The FfmpegRunner introduced in `clean-ffmpeg-integration` works but has rough edges: public types that should be internal, a faked ExitCode, unbounded stderr collection, and fragile string-based argument tests. This polish pass tightens the API surface, adds robustness, and aligns test coverage with what's actually our logic vs. FFMpegCore's.

## What Changes

- Add `DownloadServiceExtensions` with `AddFfmpegRunner()` extension method for DI registration
- Make `IFfmpegRunner`, `FfmpegRunner`, `FfmpegResult`, `ProgressUpdate` all internal
- Switch to `throwOnError: true` + catch `FFMpegException` to get real exit code and stderr
- Cap stderr collection to last 50 lines to prevent unbounded memory growth
- Make `BuildArguments` private (was `internal static` only for tests)
- Delete argument string tests (`BuildArguments_*` tests) — they test FFMpegCore, not our code
- Keep progress parsing tests (`ParseProgressLine_*` tests) — those test our logic

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `ffmpeg-process`: Error handling requirement changes — runner now captures real exit code via exception instead of faked value, and stderr is capped to last 50 lines.

## Impact

- **FunkArr.Download**: FfmpegRunner.cs, IFfmpegRunner.cs updated (visibility, error handling, stderr cap)
- **FunkArr.Download**: New `DownloadServiceExtensions.cs` for DI registration
- **FunkArr.Download.Tests**: FfmpegRunnerTests.cs — argument tests removed, progress tests kept
- **FunkArr (Host)**: ServiceSetupContainer simplified — `services.AddFfmpegRunner()` replaces direct registration + `using FunkArr.Download`
