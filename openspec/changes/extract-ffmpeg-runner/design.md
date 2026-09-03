## Context

DownloadWorker currently owns two responsibilities: persistent state management and FFmpeg process orchestration. The orchestration code (`StartFfmpeg`, `CancelFfmpeg`, process fields, `Task.Run` + `PipeTo`, progress block parsing) is ~80 lines mixed into the actor. The FFmpeg utility classes (`FfmpegProcess`, `FfmpegArgumentBuilder`, `FfmpegProgressParser`, `FfmpegProgress`) are already well-factored but consumed directly by the actor.

## Goals / Non-Goals

**Goals:**

- Single static entry point for FFmpeg orchestration: `FfmpegRunner.Run(self, videoUrl, subtitleUrl, outputPath)`
- Worker has zero FFmpeg knowledge beyond receiving `ProgressUpdate` / `ProcessExited` and calling `CancellationTokenSource.Cancel()`
- Flatten `FfmpegProgress` record into `ProgressUpdate` message — remove unnecessary wrapping layer
- Existing utility classes (`FfmpegProcess`, `FfmpegArgumentBuilder`, `FfmpegProgressParser`) remain unchanged as internal implementation details

**Non-Goals:**

- Changing FFmpeg argument structure or progress parsing logic
- Modifying persistence events or state shape
- Making FFmpeg runner injectable/mockable (it's a static facade over a process — integration tests cover this)
- Changing the download-worker spec's observable behavior

## Decisions

### Static facade over child actor

`FfmpegRunner` is a static class with a `Run` method, not a child actor. The runner takes `IActorRef self` and sends messages back via `self.Tell`.

**Why not a child actor:** The existing `Task.Run` + `PipeTo` pattern is functionally equivalent and simpler. A child actor adds supervision ceremony without benefit — the worker already handles all exit codes and errors. The static facade just moves the orchestration code out of the actor into a cohesive unit.

### Messages as top-level internal records

`ProgressUpdate` and `ProcessExited` are `internal sealed record` types in their own file (`FfmpegRunner.cs` or a companion), not nested inside the runner. They are the contract between runner and worker.

```
internal sealed record ProgressUpdate(long TotalSize, long OutTimeUs, double Speed);
internal sealed record ProcessExited(int ExitCode, string? ErrorOutput, int ElapsedSeconds);
```

**Why not nested in FfmpegRunner:** Static classes can have nested types, but these records are consumed by `Receive<T>()` in the worker — keeping them top-level is cleaner for the handler registrations.

### Return CancellationTokenSource from Run

`FfmpegRunner.Run` returns a `CancellationTokenSource` so the worker can cancel the process. The runner creates and owns the `Task.Run` internally.

**Why CTS return:** The worker needs exactly one capability — cancel. Returning the CTS is the minimal contract. No `IDisposable` wrapper needed since CTS disposal happens in the worker's existing `CancelFfmpeg` → renamed to just cancel logic.

### Remove FfmpegProgress record

The `FfmpegProgress` record (`OutTimeUs`, `TotalSize`, `Speed`, `IsEnd`) is only used as an intermediate between parser and the actor's `Tell`. With the runner owning the parsing, `ProgressUpdate` replaces it directly. The `IsEnd` field was never used — block completeness is determined by the process exiting.

## Risks / Trade-offs

- **Static makes unit testing harder** → Acceptable: FFmpeg orchestration is inherently an integration concern. The worker can be tested with direct `Tell` of `ProgressUpdate`/`ProcessExited` messages, which is actually easier than before.
- **Runner still uses Task.Run internally** → This is fine. The runner encapsulates it; the worker doesn't see it.
