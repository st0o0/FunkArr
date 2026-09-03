## Why

DownloadWorker mixes two concerns: persistent state management (commands, events, recovery) and FFmpeg process orchestration (spawning, progress reading, cancellation). The 60-line `StartFfmpeg` method with `Task.Run` + `PipeTo`, process lifecycle fields, and progress parsing logic makes the actor harder to read, test, and extend.

## What Changes

- Extract a static `FfmpegRunner` facade that encapsulates all FFmpeg orchestration behind a single `Run(self, videoUrl, subtitleUrl, outputPath)` call returning a `CancellationTokenSource`
- Remove `FfmpegProgress` record; flatten its fields into a new `ProgressUpdate` message
- Introduce `ProcessExited` message as the completion signal from the runner
- DownloadWorker loses all FFmpeg knowledge — no `FfmpegProcess` field, no `Task.Run`, no `Stopwatch`, no `Dictionary<string,string>` block parsing
- `FfmpegProcess`, `FfmpegArgumentBuilder`, `FfmpegProgressParser` become internal implementation details behind the runner

## Capabilities

### New Capabilities

- `ffmpeg-runner`: Static facade that owns FFmpeg process lifecycle and communicates results back to a caller via `IActorRef.Tell`

### Modified Capabilities

- `ffmpeg-process`: Requirements unchanged — the runner wraps these existing primitives
- `download-worker`: Worker delegates FFmpeg orchestration to the runner instead of owning it directly

## Impact

- `src/FunkArr.Download/FfmpegRunner.cs` — new file
- `src/FunkArr.Download/FfmpegProgress.cs` — removed
- `src/FunkArr.Download/DownloadWorker.cs` — simplified, FFmpeg orchestration removed
- `src/FunkArr.Download.Tests/` — existing tests may need message type updates
