## Why

The current download architecture puts too many responsibilities into the DownloadManager (queue, history, progress, concurrency, dispatch, filename generation) while the DownloadWorker is a non-persistent sharded entity that can't survive restarts and gains nothing from sharding. Progress tracking lives on the Manager's persistent state, causing unnecessary array rebuilds on every tick. Retry is lossy because HistoryEntry doesn't carry URLs. The Worker's Task.Run + PipeTo pattern has no cancellation and leaks FFmpeg processes on actor restarts.

## What Changes

- **DownloadManager becomes a thin ledger + scheduler.** Persists only download records (ID, title, category, size, status, completion data). Manages concurrency gate. Holds progress in a non-persistent dictionary, merged at query time.
- **DownloadWorker becomes a persistent sharded entity.** Owns all operational data (URLs, output path, subtitle URL, channel, duration). Owns FFmpeg lifecycle with proper CancellationTokenSource. Owns progress state locally. Reports Completed/Failed back to Manager.
- **New message flow.** AddDownload → Manager persists record + sends InitDownload to Worker shard → Worker persists full metadata → Manager sends StartDownload (go signal, no payload) when concurrency allows → Worker runs FFmpeg → Worker pushes progress to Manager → Worker reports Completed/Failed.
- **Recovery is clean.** Worker resets Downloading→Initialized on recovery (has all data to retry). Manager resets Processing→Queued and re-dispatches StartDownload signals.
- **Retry is lossless.** Worker has all URLs persisted. Manager tells Worker to reset, then re-sends StartDownload.
- **Cancellation via CancelDownload command.** Manager sends CancelDownload to Worker, Worker cancels CTS, kills FFmpeg, passivates.
- **Progress removed from DownloadEntry/persistent state.** Manager keeps Dict<Guid, ProgressInfo> in memory, never persisted.
- **Persistence events restructured.** Manager events: DownloadRegistered, DownloadStatusChanged, DownloadRemoved. Worker events: DownloadInitialized, DownloadStarted, DownloadSucceeded, DownloadFailed.
- **BREAKING**: Existing download journal entries are incompatible. Full journal reset required (acceptable at 0.x).

## Capabilities

### New Capabilities

_None — this is a redesign of existing capabilities._

### Modified Capabilities

- `download-manager`: Responsibility narrowed to ledger + scheduling. State model changes from Queue/History lists to flat record list. Progress moves to non-persistent side-channel. New message handling (InitDownload forwarding, CancelDownload).
- `download-worker`: Becomes persistent. Owns full download metadata and lifecycle. New two-phase init (InitDownload → StartDownload). Proper FFmpeg cancellation. Self-contained recovery.
- `download-messages`: New messages (InitDownload, CancelDownload, WorkerStatus). StartDownload becomes a bare go-signal (no payload). DownloadProgress originates from Worker to Manager as push. Persistence DTOs restructured.

## Impact

- `FunkArr.Download/` — DownloadManager.cs, DownloadManagerState.cs, DownloadWorker.cs, DownloadWorkerState.cs all rewritten
- `FunkArr.Messages/Download/` — new messages (InitDownload, CancelDownload), StartDownload simplified, new persistence events for Worker
- `FunkArr.Persistence/Events/Download/` — new Worker persistence DTOs (DownloadInitialized, DownloadStarted, DownloadSucceeded, DownloadFailed), Manager events renamed/restructured
- `FunkArr/Configuration/AkkaSetupContainer.cs` — Worker shard registration unchanged, but Worker now needs persistence config
- `FunkArr.ArrApi/Sabnzbd/` — no changes expected (talks to Manager via same query messages)
- `FunkArr.Download.Tests/` — tests updated for new message flow and persistent Worker
- Journal data — **full reset required**, incompatible with previous events
