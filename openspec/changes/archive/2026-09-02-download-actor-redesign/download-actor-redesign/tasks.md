## 1. Messages & Persistence DTOs

- [x] 1.1 Add new messages: `SlotFree`, `QueryWorkerStatus`, `WorkerStatusResult`, `RecordDownload`, `RemoveHistoryEntry` in `FunkArr.Messages/Download`
- [x] 1.2 Add `IDownloadHistory` marker interface in `FunkArr.Core`
- [x] 1.3 Add new Manager persistence DTOs: `DownloadEnqueued`, `DownloadDispatched`, `DownloadDequeued` in `FunkArr.Persistence/Events/Download`
- [x] 1.4 Add new HistoryActor persistence DTOs: `HistoryRecorded`, `HistoryRemoved` in `FunkArr.Persistence/Events/Download`
- [x] 1.5 Remove obsolete messages: `DownloadProgress`, `DownloadCompleted`, `DownloadFailed` (the message, not the persistence DTO) from `FunkArr.Messages/Download`
- [x] 1.6 Remove obsolete persistence DTOs: `DownloadRegistered`, `DownloadStatusChanged`, `DownloadRemoved` from `FunkArr.Persistence/Events/Download`

## 2. DownloadManager Rewrite

- [x] 2.1 Rewrite `DownloadManagerState` — state is two collections: `Queued` (ordered list of Guid) and `Dispatched` (set of Guid). Apply methods for `DownloadEnqueued`, `DownloadDispatched`, `DownloadDequeued`. Remove `DownloadRecord`, `ProgressInfo`, all old Apply methods
- [x] 2.2 Rewrite `DownloadManager` — handle `AddDownload` (enqueue + init worker), `SlotFree` (dequeue + dispatch next), `DeleteDownload` (dequeue + cancel), `RetryDownload` (enqueue + reset worker). Remove progress tracking, history handling
- [x] 2.3 Implement `QueryQueue` fan-out — send `QueryWorkerStatus` to all Workers in Queued/Dispatched sets, collect responses with 2s timeout, build `QueueResult`
- [x] 2.4 Rewrite `DownloadManagerStateTests` for new state shape

## 3. DownloadWorker Changes

- [x] 3.1 Add in-memory progress fields to `DownloadWorkerState` (BytesDownloaded, CurrentTimeUs, Speed)
- [x] 3.2 Add `QueryWorkerStatus` handler — respond with `WorkerStatusResult` containing state + progress
- [x] 3.3 Change completion/failure handlers — send `SlotFree` to Manager and `RecordDownload` to HistoryActor instead of `DownloadCompleted`/`DownloadFailed`
- [x] 3.4 Remove progress push to Manager — `FfmpegProgressTick` updates in-memory state only
- [x] 3.5 Update `DownloadWorkerStateTests` for new progress fields

## 4. DownloadHistoryActor

- [x] 4.1 Create `DownloadHistoryActorState` — list of `HistoryRecord`, Apply methods for `HistoryRecorded` and `HistoryRemoved`
- [x] 4.2 Create `DownloadHistoryActor` — persistent singleton handling `RecordDownload`, `RemoveHistoryEntry`, `QueryHistory`
- [x] 4.3 Register `DownloadHistoryActor` as Cluster Singleton in startup (actor registration with `IDownloadHistory`)
- [x] 4.4 Add `DownloadHistoryActorStateTests`

## 5. API Routing

- [x] 5.1 Update `DownloadApiEndpoints` — resolve `IDownloadHistory` alongside `IDownloadManager`. Route `history` queries to HistoryActor, `history delete` to HistoryActor, `retry` to Manager + HistoryActor
- [x] 5.2 Update `fullstatus` endpoint to work with fan-out-based queue result

## 6. Build & Verify

- [x] 6.1 Run `dotnet build` and fix compilation errors
- [x] 6.2 Run `dotnet format` across all edited projects
- [x] 6.3 Run all download domain tests
