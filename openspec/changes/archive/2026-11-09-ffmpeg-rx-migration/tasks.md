## 1. Dependency Swap

- [x] 1.1 Replace FFMpegCore with FFmpeg.Rx in `Directory.Packages.props` (remove FFMpegCore PackageVersion, add FFmpeg.Rx PackageVersion)
- [x] 1.2 Update `FunkArr.Download.csproj` PackageReference from FFMpegCore to FFmpeg.Rx
- [x] 1.3 Verify solution builds after dependency swap (expect compile errors only in FfmpegProcess.cs)

## 2. Rewrite FfmpegProcess

- [x] 2.1 Rewrite `FfmpegProcess.ExecuteAsync` to use `Ffmpeg.Input(new Uri(videoUrl))` builder with `InputOptionConfig(CustomArgs: [...])` for user-agent and proxy, `.WithProgressReporting()`, and `.Output(outputPath, o => ...)` with typed stream builders
- [x] 2.2 Add subtitle stream via `.Input(subtitlePath)` and `.Stream(1, StreamType.Subtitle, s => s.Encode(SubtitleCodec.Srt).Disposition("0").Metadata("language", lang))` when subtitle path is present
- [x] 2.3 Add HLS BSF tolerance via `o.CustomArg("-bsf:a aac_adtstoasc=no_validation=1")` when `input.IsHls`
- [x] 2.4 Subscribe to `session.Progress` and bridge `ProgressEvent` → `ProgressUpdate` (BytesWritten→TotalSize, TimeUs→OutTimeUs, Speed→Speed)
- [x] 2.5 Map `FFmpeg.Rx.Models.FfmpegResult` → FunkArr `FfmpegResult` (Duration→ElapsedSeconds, FailureKind exhaustive switch: Unknown→Permanent, Transient→Transient, Permanent→Permanent)
- [x] 2.6 Use `await using` for IFfmpegSession disposal
- [x] 2.7 Remove `BuildArguments`, `ParseProgressLine`, `ExtractError`, `ClassifyFailure`, `ParseSpeed`, `GetLong` methods (now in FFmpeg.Rx)

## 3. Update Tests

- [x] 3.1 Remove `ParseProgressLine_*` tests (14 tests — covered by FFmpeg.Rx.Tests)
- [x] 3.2 Remove `ExtractError_*` tests (4 tests — covered by FFmpeg.Rx.Tests)
- [x] 3.3 Remove `ClassifyFailure_*` tests (7 tests — covered by FFmpeg.Rx.Tests)
- [x] 3.4 Rewrite `BuildArguments_*` tests to verify the new builder produces correct serialized arguments (same behavioral assertions: stream mapping, codec copy, BSF tolerance, subtitle args, proxy, user-agent)
- [x] 3.5 Verify `RemuxOptions_*` tests still pass unchanged

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` — zero errors, zero warnings
- [x] 4.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 4.3 Run `dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj` — all tests pass
