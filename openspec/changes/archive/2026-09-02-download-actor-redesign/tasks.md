## 1. Messages & Persistence DTOs

- [x] 1.1 Add new messages: `InitDownload`, `CancelDownload`, `ResetDownload` in `FunkArr.Messages/Download/`
- [x] 1.2 Simplify `StartDownload` to bare go-signal (DownloadId only)
- [x] 1.3 Simplify `DownloadStatus` enum — remove unused `Extracting`, `Moving`, `Verifying` values
- [x] 1.4 Add Worker persistence DTOs in `FunkArr.Persistence/Events/Download/`: `DownloadInitialized`, `DownloadStarted`, `DownloadSucceeded`
- [x] 1.5 Add Manager persistence DTO `DownloadRegistered` (replaces `DownloadQueued`)
- [x] 1.6 Remove `DownloadQueued` persistence DTO

## 2. DownloadWorker — Persistent Sharded Entity

- [x] 2.1 Rewrite `DownloadWorkerState` — full metadata (Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category, OutputPath), WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage; Apply methods for each Worker event
- [x] 2.2 Rewrite `DownloadWorker` as `ReceivePersistentActor` — PersistenceId = `"download-{entityId}"`, handle `InitDownload` (persist DownloadInitialized), `StartDownload` (persist DownloadStarted, launch FFmpeg), `CancelDownload` (cancel + passivate), `ResetDownload` (reset to Initialized)
- [x] 2.3 Implement CancellationTokenSource lifecycle — create on StartDownload, cancel in PreStop/CancelDownload, pass token to FFmpeg reading Task
- [x] 2.4 Implement Worker recovery — replay events to rebuild state, reset Downloading→Initialized, passivate if Completed/Failed
- [x] 2.5 Keep FFmpeg progress parsing + subtitle retry logic (moved from old Worker, adapted to use persisted state and CTS)

## 3. DownloadManager — Thin Ledger + Scheduler

- [x] 3.1 Rewrite `DownloadManagerState` — flat list of `DownloadRecord` (Id, Title, Category, Size, Status, FilePath?, FailMessage?, CompletedAt?, DownloadTimeSeconds?), Apply methods for DownloadRegistered/DownloadStatusChanged/DownloadRemoved
- [x] 3.2 Rewrite `DownloadManager` — persist DownloadRegistered on AddDownload, forward InitDownload to shard region, generate output path (filename sanitization stays here)
- [x] 3.3 Implement non-persistent progress dictionary — `Dictionary<Guid, ProgressInfo>`, updated on DownloadProgress, cleaned on Completed/Failed, merged into QueryQueue response
- [x] 3.4 Simplify DispatchNext — pick from Queued records, persist status→Processing, send bare StartDownload to shard
- [x] 3.5 Implement HandleRetry — persist status Failed→Queued, send ResetDownload to Worker shard, DispatchNext
- [x] 3.6 Implement HandleDelete — persist DownloadRemoved, send CancelDownload to Worker shard, clean progress dict
- [x] 3.7 Implement recovery — rebuild records from events, reset Processing→Queued, clear progress dict, DispatchNext

## 4. Adapt API Layer

- [x] 4.1 Update `QueueResult`/`QueueItem` construction to merge records with progress dict
- [x] 4.2 Update `HistoryResult`/`HistoryItem` construction from flat record list (filter by Completed/Failed status)
- [x] 4.3 Verify SABnzbd adapter endpoints still work with updated message types (no changes expected, but verify)

## 5. Tests

- [x] 5.1 Update DownloadWorker tests — persistent actor lifecycle (InitDownload → StartDownload → Completed/Failed), recovery scenarios, CancelDownload, ResetDownload
- [x] 5.2 Update DownloadManager tests — AddDownload flow (register + forward InitDownload), dispatch logic, progress side-channel, retry, delete
- [x] 5.3 Verify architecture tests pass (Worker is now persistent, naming conventions hold)
- [x] 5.4 Run full test suite and format check
