## Context

The download domain has two actors: DownloadManager (Cluster Singleton, persistent) and DownloadWorker (Sharded Entity, non-persistent). The Manager owns too many concerns — queue, history, progress, concurrency, dispatch, filename generation. The Worker is a sharded entity that gains nothing from sharding: it's non-persistent, can't survive restarts, and holds an OS process that's inherently node-local. Progress tracking rebuilds the entire queue array on every FFmpeg tick. Retry loses URL data because HistoryEntry doesn't carry it. The Worker's Task.Run + PipeTo has no cancellation, leaking FFmpeg processes on actor restarts.

## Goals / Non-Goals

**Goals:**
- DownloadManager becomes a thin ledger + scheduler — only persists download records and manages concurrency
- DownloadWorker becomes a persistent sharded entity that owns all operational data and FFmpeg lifecycle
- Progress tracking moves out of persistent state entirely
- Proper FFmpeg cancellation via CancellationTokenSource tied to actor lifecycle
- Lossless retry (Worker has all URLs persisted)
- Clean recovery semantics on both sides

**Non-Goals:**
- Multi-node distribution (still single-node cluster sharding)
- Download resume (FFmpeg can't resume HTTP streams)
- Priority queue ordering (future work)
- Changing the SABnzbd API adapter layer (QueryQueue/QueryHistory responses stay compatible)

## Decisions

### Decision: Worker owns all download metadata (persistent)

The Worker persists its full download spec (URLs, title, channel, duration, size, category, output path) as its first event on initialization. This makes the Worker self-contained — it can recover from any state without external data.

**Alternative considered:** Manager persists full data, Worker is ephemeral and re-initialized on recovery. Rejected because it keeps the Manager fat and the Worker can't self-recover — the Manager must track which Workers need re-initialization.

### Decision: Two-phase dispatch (InitDownload → StartDownload)

The Manager sends `InitDownload` immediately when a download is added — this carries all metadata to the Worker. `StartDownload` is a separate bare go-signal sent only when concurrency allows. This separates "Worker knows about this download" from "Worker should start processing."

**Alternative considered:** Single message with all data sent only when concurrency allows. Rejected because the Worker wouldn't exist until dispatch, making queries about queued downloads impossible at the Worker level.

### Decision: Progress as non-persistent side-channel on Manager

The Manager keeps a `Dictionary<Guid, ProgressInfo>` that is never persisted. Workers push `DownloadProgress` to the Manager periodically. On `QueryQueue`, the Manager merges its persistent records with the progress dict.

**Alternative considered:** Query Workers directly via Ask pattern. Rejected because it adds latency, timeout complexity, and doesn't work for passivated Workers.

### Decision: Manager state as flat record list (not Queue/History split)

The Manager stores a single list of `DownloadRecord` with a status field. Queue = records where status is Queued or Processing. History = records where status is Completed or Failed. This eliminates the move-between-lists logic that currently causes bugs in retry.

**Alternative considered:** Keep Queue/History split. Rejected because the split is a view concern, not a storage concern — it caused the retry data loss bug.

### Decision: Worker recovery resets Downloading → Initialized

On recovery, if the Worker's last persisted status is Downloading, it resets to Initialized and waits for a fresh StartDownload from the Manager. The Manager independently resets Processing → Queued on its recovery and re-dispatches.

Both sides agree: anything mid-download is retried from scratch. FFmpeg can't resume, so this is the only correct behavior.

### Decision: CancelDownload as explicit command

Delete/cancel sends a `CancelDownload` message to the Worker shard. The Worker cancels its CancellationTokenSource, which kills the FFmpeg process, then passivates. This is cleaner than relying on shard passivation timeouts.

### Decision: Filename generation moves to Manager

The Manager generates the output path (combining download path + sanitized title) and includes it in `InitDownload`. The Worker doesn't need to know about the download directory configuration. This keeps filesystem config in one place.

## Risks / Trade-offs

- **Dual persistence** — Both Manager and Worker persist events for the same download. The Manager's record is the authority for queue position and status. The Worker's record is the authority for operational data. If they disagree, the Manager wins (it's the coordination point). → Mitigation: Worker reports status changes back to Manager, which persists them. The Worker never makes status decisions the Manager doesn't know about.

- **Journal reset required** — New persistence events are incompatible with current journal. → Mitigation: Version is 0.x, clean breaks are acceptable. Document in release notes.

- **Progress data loss on restart** — The non-persistent progress dict is lost on Manager restart. Downloads that were in-progress will show 0% until the re-dispatched Workers start sending progress again. → Mitigation: Acceptable — progress is cosmetic, and downloads restart from the beginning anyway.

- **Orphaned Workers** — If the Manager removes a download record but the CancelDownload message doesn't reach the Worker (network partition, timing), the Worker continues running FFmpeg. → Mitigation: Workers should have a passivation timeout. If no progress query or StartDownload arrives within N minutes, passivate. This is a safety net, not the primary mechanism.
