## Context

Load testing with production Docker limits (0.5 CPU, 512 MB) showed 68% CPU
throttling from FFmpeg thread contention and identified unbounded Postgres
journal growth in three singleton actors.

## Goals / Non-Goals

**Goals:**
- Reduce FFmpeg CPU overhead under cgroup limits
- Bound Postgres journal/snapshot table growth for singleton actors

**Non-Goals:**
- Changing the MKV output format or download pipeline
- Adding journal cleanup for sharded entities (DownloadWorker has ~4 events per
  entity — the rows are cheap and cleanup adds complexity)
- Dynamic concurrency adjustment based on CPU pressure

## Decisions

### FFmpeg thread limit
Add `-threads 1` as an input option in `FfmpegProcess.BuildArguments`.

**Why input option:** FFMpegCore's `InputOptions` callback maps to arguments
before `-i`, which is where FFmpeg's global thread setting belongs. Placing it
on output options would only affect the muxer, not the demuxer/HTTP reader.

**Alternative considered:** `-threads 0` (auto) — this is the current default
and causes FFmpeg to spawn ~4 threads for I/O and demuxing, which competes
with itself under a 0.5 CPU quota.

### Journal cleanup pattern
Replace the no-op `SaveSnapshotSuccess` handler in all three singletons with
`DeleteMessages` + `DeleteSnapshots`. This is the standard Akka.NET pattern
for bounded journal growth.

**Why not a shared base class:** Each actor already handles `SaveSnapshotSuccess`
inline. Adding a base class for two lines of code is over-abstraction.

## Risks / Trade-offs

- [Risk] `-threads 1` could slow HLS segment fetching where FFmpeg benefits
  from parallel I/O → HLS downloads are still network-bound; measured download
  times are dominated by HTTP latency, not demuxer parallelism. Verify with a
  few HLS downloads after the change.
- [Risk] `DeleteMessages` removes journal entries that might be useful for
  debugging → Snapshots capture the full state; individual events are not
  needed once the snapshot exists. Structured logging provides the audit trail.

## Migration Plan

No migration needed. Changes are backward-compatible:
- Thread limit is transparent to FFmpeg behavior (stream-copy throughput is I/O-bound)
- Journal cleanup only removes entries older than the latest snapshot; recovery
  still works identically via snapshot + replay of recent events
