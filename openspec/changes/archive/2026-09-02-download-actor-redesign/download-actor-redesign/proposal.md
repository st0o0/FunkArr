## Why

The DownloadManager is a god object — it manages the queue, tracks progress, stores history, dispatches workers, and answers all API queries. This forces every API call through a singleton bottleneck and duplicates download data already persisted by Workers. The SearchManager demonstrates the right pattern: a coordinator that routes, merges, and forgets. The download domain should follow the same separation.

## What Changes

- **BREAKING** DownloadManager becomes a pure coordinator: queue membership (enqueue/dispatch/dequeue) and concurrency control only. Removes all status/history tracking and progress storage.
- **BREAKING** DownloadManager persistence events change from `DownloadRegistered`/`DownloadStatusChanged`/`DownloadRemoved` to `DownloadEnqueued`/`DownloadDispatched`/`DownloadDequeued`.
- DownloadWorker gains `QueryWorkerStatus` handling and holds progress in-memory instead of pushing to Manager.
- DownloadWorker notifies both Manager (`SlotFree`) and HistoryActor (`RecordDownload`) on completion/failure.
- New `DownloadHistoryActor` (Cluster Singleton, Persistent) serves as read-side projection for history queries.
- API routing changes: `QueryHistory` goes directly to HistoryActor; `QueryQueue` triggers fan-out from Manager to Workers.
- New messages: `SlotFree`, `QueryWorkerStatus`, `WorkerStatusResult`, `RecordDownload`, `RemoveHistoryEntry`.
- Removed messages: `DownloadProgress` (Worker→Manager push), `DownloadCompleted`/`DownloadFailed` (replaced by `SlotFree`).
- Removed persistence events: `DownloadRegistered`, `DownloadStatusChanged`, `DownloadRemoved`.

## Capabilities

### New Capabilities

- `download-history`: Read-side projection actor for download history queries. Persistent Cluster Singleton that receives completion/failure notifications from Workers and serves QueryHistory.

### Modified Capabilities

- `download-manager`: Changes from god object to pure coordinator. Queue membership + concurrency control only. Fan-out queries to Workers.
- `download-worker`: Adds QueryWorkerStatus, progress ownership, dual notification (Manager + HistoryActor) on completion.
- `download-messages`: New messages for the redesigned flow, removed obsolete messages and persistence events.

## Impact

- `FunkArr.Download`: DownloadManager.cs, DownloadManagerState.cs, DownloadWorker.cs, DownloadWorkerState.cs — major rewrite. New DownloadHistoryActor.cs + DownloadHistoryActorState.cs.
- `FunkArr.Messages/Download`: New messages (SlotFree, QueryWorkerStatus, WorkerStatusResult, RecordDownload, RemoveHistoryEntry). Remove DownloadProgress. Replace DownloadCompleted/DownloadFailed with SlotFree.
- `FunkArr.Persistence/Events/Download`: New events (DownloadEnqueued, DownloadDispatched, DownloadDequeued, HistoryRecorded, HistoryRemoved). Remove DownloadRegistered, DownloadStatusChanged, DownloadRemoved.
- `FunkArr.ArrApi/Sabnzbd/DownloadApiEndpoints.cs`: Route history queries to HistoryActor instead of Manager. Delete routing needs both Manager (dequeue) and HistoryActor (remove entry).
- `FunkArr/Startup`: Register DownloadHistoryActor as Cluster Singleton.
- `FunkArr.Download.Tests`: Rewrite DownloadManagerStateTests, DownloadWorkerStateTests. New DownloadHistoryActorStateTests.
- Breaking change to persistence journals — existing download data will not migrate (acceptable at version 0.x).
