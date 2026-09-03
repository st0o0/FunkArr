## 1. Messages

- [x] 1.1 Create `ProgressUpdate` and `ProcessExited` internal records in `FfmpegRunner.cs`
- [x] 1.2 Delete `FfmpegProgress.cs`

## 2. FfmpegRunner

- [x] 2.1 Create static `FfmpegRunner` class with `Run(IActorRef self, string videoUrl, string? subtitleUrl, string outputPath)` returning `CancellationTokenSource`
- [x] 2.2 Move `Task.Run` orchestration logic from DownloadWorker into `FfmpegRunner.Run` — process start, progress reading, `PipeTo`
- [x] 2.3 Update `FfmpegProgressParser` to work without `FfmpegProgress` (return values used directly in `ProgressUpdate`)

## 3. DownloadWorker cleanup

- [x] 3.1 Replace `StartFfmpeg` method with `FfmpegRunner.Run` call, remove `_ffmpeg` field
- [x] 3.2 Replace `FfmpegProgressTick` / `FfmpegCompleted` handlers with `ProgressUpdate` / `ProcessExited` handlers
- [x] 3.3 Simplify `CancelFfmpeg` to only cancel/dispose the CTS (no `FfmpegProcess` reference)
- [x] 3.4 Remove `FfmpegProcess` and `Stopwatch` imports/fields from DownloadWorker

## 4. Tests

- [x] 4.1 Update `DownloadWorkerStateTests` if any reference old message types
- [x] 4.2 Verify build and run all Download tests
- [x] 4.3 Run `dotnet format` and fix any violations
