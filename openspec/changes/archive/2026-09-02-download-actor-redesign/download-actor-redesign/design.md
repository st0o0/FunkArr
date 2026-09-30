## Context

The DownloadManager is currently a god object: it manages queue membership, tracks progress, stores full download history, dispatches workers, and serves all API queries. Every SABnzbd API call (`queue`, `history`, `fullstatus`, `delete`, `retry`) routes through this singleton via `Ask<>`, making it a serialization bottleneck. It persists a `DownloadStatusChanged` god event that carries optional fields for multiple distinct transitions.

Meanwhile, the SearchManager demonstrates the correct coordinator pattern: route to workers, collect responses, reply, forget. The download domain should follow the same separation.

The DownloadWorker already has clean, specific persistence events (`DownloadInitialized`, `DownloadStarted`, `DownloadSucceeded`, `DownloadFaulted`). The Manager duplicates this data via its own journal.

## Goals / Non-Goals

**Goals:**
- DownloadManager becomes a pure coordinator (queue + concurrency) following the SearchManager pattern
- Single source of truth per concern: Worker owns lifecycle, HistoryActor owns history view
- Eliminate duplicated state between Manager and Worker journals
- Replace the `DownloadStatusChanged` god event with specific events
- Remove the singleton bottleneck for history queries

**Non-Goals:**
- Migrating existing persistence journals (breaking change, acceptable at 0.x)
- Changing the SABnzbd API wire format (adapter stays compatible)
- Changing the FFmpeg pipeline or Worker internals (only the communication layer changes)
- Adding a separate read-side for queue queries (fan-out to max ~8 Workers is sufficient)

## Decisions

### D1: Manager persists only queue membership

The Manager's journal records three events: `DownloadEnqueued(DownloadId)`, `DownloadDispatched(DownloadId)`, `DownloadDequeued(DownloadId)`. State after recovery is two sets: `Queued` and `Dispatched`. On recovery, Dispatched items reset to Queued (same as current `ResetProcessing`).

**Why not stateless like SearchManager?** Search requests are ephemeral — losing them on restart is fine. Download queue position must survive restarts. Minimal persistence (just IDs) keeps the journal small.

**Alternative: persist full AddDownload metadata in DownloadEnqueued.** Rejected — the Worker already persists `DownloadInitialized` with all metadata. The Manager only needs to know the ID exists in the queue.

### D2: Worker holds progress, responds to QueryWorkerStatus

Instead of pushing `DownloadProgress` ticks to the Manager, the Worker stores its latest progress in-memory. A new `QueryWorkerStatus` message lets the Manager (or anyone) ask for current state including progress. The Worker replies with `WorkerStatusResult` containing all fields needed for both `QueueItem` and `HistoryItem` rendering.

**Why not keep progress push?** The push model forced the Manager to maintain a `Dictionary<Guid, ProgressInfo>` that was lost on restart and tightly coupled Worker→Manager. Pull on demand is cleaner and aligns with the coordinator pattern.

### D3: Worker notifies Manager and HistoryActor on completion

On completion or failure, the Worker sends:
- `SlotFree(DownloadId)` to Manager — triggers dequeue + dispatch next
- `RecordDownload(DownloadId, Title, Category, Size, Status, FilePath, FailMessage, DownloadTimeSeconds, CompletedAt)` to HistoryActor — feeds the read model

**Why two messages instead of one?** Manager and HistoryActor have different concerns. Manager needs to know a slot is free (DownloadId only). HistoryActor needs the full summary. One message to each keeps them decoupled.

### D4: HistoryActor as persistent Cluster Singleton

The HistoryActor is a new Cluster Singleton with its own persistence journal. Events: `HistoryRecorded(...)` and `HistoryRemoved(DownloadId)`. It serves `QueryHistory` directly — no Manager involvement.

**Why persistent instead of rebuilt from Workers?** After restart, passivated Workers aren't discoverable (sharding has no "list all entities"). The Manager could keep completed IDs, but that re-introduces tracking outside the queue concern. Own persistence is clean and self-contained.

**Why not Akka Persistence Query (read journal directly)?** Adds infrastructure coupling. The HistoryActor receiving explicit messages is simpler, testable, and doesn't depend on journal provider features.

### D5: QueryQueue uses fan-out from Manager to Workers

When the Manager receives `QueryQueue`, it sends `QueryWorkerStatus` to each Worker in its `Queued ∪ Dispatched` set, collects responses, and builds the `QueueResult`.

At most ~8 concurrent asks (maxConcurrent + a few queued). This mirrors the SearchManager's fan-out pattern for `SearchType.Both`.

The Manager uses `Task.WhenAll` with a short timeout (2s) to collect Worker responses. Workers that don't respond in time are shown with zero progress.

### D6: Delete and Retry routing

**Delete from queue:** Manager handles `DeleteDownload` — persists `DownloadDequeued`, tells Worker `CancelDownload`, replies. If the item is in history (not in Manager's queue), the request goes to HistoryActor for `RemoveHistoryEntry`.

**API layer determines routing:** The adapter checks which actor to ask based on context. For SABnzbd compatibility, `mode=queue&name=delete` goes to Manager, `mode=history&name=delete` goes to HistoryActor.

**Retry:** Manager handles `RetryDownload` — but now asks HistoryActor to remove the history entry and enqueues the download again. Worker gets `ResetDownload`.

### D7: API endpoint routing changes

```
Endpoint            Before              After
─────────────────────────────────────────────────────
POST addfile        Manager             Manager (unchanged)
GET  queue          Manager             Manager (fan-out to Workers)
GET  history        Manager             HistoryActor (direct)
GET  fullstatus     Manager             Manager (fan-out to Workers)
queue delete        Manager             Manager
history delete      Manager             HistoryActor
retry               Manager             Manager + HistoryActor
```

The adapter resolves both `IDownloadManager` and `IDownloadHistory` from the actor registry.

## Risks / Trade-offs

**Fan-out latency for queue queries** — Sonarr polls queue every few seconds. Fan-out to ~5-8 Workers adds a few ms vs reading local state. At this scale (single-node, low volume), this is negligible. → If it becomes a problem, the Manager could cache last-known progress with a short TTL.

**Two actors notified on completion** — Worker must tell both Manager and HistoryActor. If HistoryActor is temporarily unavailable, history could be lost. → HistoryActor is a singleton on the same node; unavailability implies the entire system is down. At-most-once delivery is acceptable here.

**Journal migration not supported** — Existing download history and queue state will be lost on upgrade. → Acceptable at version 0.x per project rules. Users restart with an empty queue.

**HistoryActor journal grows unbounded** — Every completed download adds a `HistoryRecorded` event. → Add snapshot support (T2 persistence tier). Snapshot after every N events. Future concern, not blocking.
