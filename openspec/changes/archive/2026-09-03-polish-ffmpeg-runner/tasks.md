## 1. DI Extension Method

- [x] 1.1 Create `DownloadServiceExtensions.cs` in FunkArr.Download with `AddFfmpegRunner()` extension method
- [x] 1.2 Update ServiceSetupContainer to use `services.AddFfmpegRunner()`, remove `using FunkArr.Download`

## 2. Visibility

- [x] 2.1 Make `IFfmpegRunner`, `FfmpegResult`, `ProgressUpdate` internal — adjusted: kept public (DownloadWorker's public constructor requires accessible parameter types), only impl is internal
- [x] 2.2 Make `FfmpegRunner` class internal

## 3. Error Handling

- [x] 3.1 Switch to `throwOnError: true`, catch `FFMpegException` for stderr via `FFMpegErrorOutput`, remove `NotifyOnError` and `stderrLines`/`JoinStderr`
- [x] 3.2 Add `CapStderr` method (last 4096 chars), replace `TruncateError` usage in DownloadWorker with reliance on pre-capped runner output

## 4. Cleanup

- [x] 4.1 Make `BuildArguments` private (was `internal static`)
- [x] 4.2 Delete argument string tests (`BuildArguments_video_only_*`, `BuildArguments_with_subtitle_*`, `BuildArguments_hls_*`) from FfmpegRunnerTests, keep progress parsing tests

## 5. Verify

- [x] 5.1 Build solution, run all tests, verify dotnet format
