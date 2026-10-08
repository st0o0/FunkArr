## Why

Load testing with production Docker limits (0.5 CPU, 512 MB RAM) revealed two issues:
FFmpeg is throttled 68% of the time because it spawns multiple internal threads that
compete for a half-core quota, and Akka persistence journal tables in Postgres grow
unbounded because no actor calls `DeleteMessages`/`DeleteSnapshots` after snapshotting.
Neither is a correctness bug today, but both degrade performance and storage over time.

## What Changes

- Add `-threads 1` to FFmpeg input arguments in `FfmpegProcess.BuildArguments`.
  Stream-copy is I/O-bound; extra threads only increase scheduler contention under
  cgroup CPU limits. Reduces throttling overhead without affecting download speed.
- Add journal cleanup to all three persistent singleton actors (`DownloadManager`,
  `DownloadHistoryManager`, `HistoryWorker`). On `SaveSnapshotSuccess`, call
  `DeleteMessages(seqNr)` and `DeleteSnapshots(seqNr - 1)` to keep Postgres bounded
  at 1 snapshot + ≤25 journal rows per actor instead of growing linearly forever.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `ffmpeg-process`: Add thread limit argument to FFmpeg invocation
- `akka-persistence`: Add journal/snapshot cleanup after successful snapshot saves

## Impact

- `FunkArr.Download/FfmpegProcess.cs` — one additional `WithCustomArgument`
- `FunkArr.Download/DownloadManager.cs` — `SaveSnapshotSuccess` handler
- `FunkArr.Download/DownloadHistoryManager.cs` — `SaveSnapshotSuccess` handler
- `FunkArr.History/HistoryWorker.cs` — `SaveSnapshotSuccess` handler
- Existing tests for FFmpeg argument building need a new assertion
- No API changes, no breaking changes, no new dependencies
