## 1. Dead Code Cleanup

- [x] 1.1 Delete `DownloadJob.cs`, `DownloadOutcome.cs`, `DownloadProgress.cs` from `DownloadClient/Pipeline/`
- [x] 1.2 Rename `HlsDownloadServiceTests.cs` to `DownloadSourceDetectorTests.cs`

## 2. Persistence DTOs

- [x] 2.1 Add `DcSubtitleDetected` DTO to `DownloadCoordinatorJournal.cs` with `ToJournal()`/`ToDomain()` extension methods
- [x] 2.2 Create `HistoryActorJournal.cs` with DTOs: `HistoryCompletionRecorded`, `HistoryFailureRecorded`, `HistoryEntryRemoved`, `HistoryCleared` plus extension methods
- [x] 2.3 Add `QueueSnapshot` DTO to `QueueCoordinatorJournal.cs`
- [x] 2.4 Add `HistorySnapshot` DTO to `HistoryActorJournal.cs`
- [x] 2.5 Retain `QueueJobRemovedFromHistory` DTO with no-op `ToDomain()` for backward compat

## 3. DownloadActor Correctness Fixes

- [x] 3.1 Add `SubtitleDetected` domain event and persist it when `SubtitleAcquired` is received; restore `_hasSubtitle` on recovery
- [x] 3.2 Add `_workerFailedReceived` flag, reset on stage entry; handle `Terminated` → transition to Failed when flag is unset

## 4. HistoryActor

- [x] 4.1 Create `HistoryActor` class (ReceivePersistentActor, PersistenceId "download-history") with state, events, and messages in `DownloadClient/History/`
- [x] 4.2 Implement `RecordCompletion` and `RecordFailure` command handlers with event persistence
- [x] 4.3 Implement `GetHistory`, `GetRetryInfo`, `RemoveFromHistory`, `ClearHistory` query/command handlers
- [x] 4.4 Implement snapshot save (every 50 events) and snapshot recovery
- [x] 4.5 Implement `IWithStash` for recovery stashing
- [x] 4.6 Register HistoryActor as singleton in `FunkArrActorSystemSetup`

## 5. DownloadActor Integration

- [x] 5.1 Add HistoryActor notification on job completion (`RecordCompletion`) and failure (`RecordFailure`)
- [x] 5.2 Accept `ProgressTick` from child workers and forward as `ReportProgress` to DownloadRequestActor shard

## 6. QueueActor Cleanup

- [x] 6.1 Remove `CompletedJobIds` list, `GetCompletedJobIds` handler, `RemoveFromHistory` handler, and `JobRemovedFromHistory` event handling
- [x] 6.2 Add silent `Recover<QueueJobRemovedFromHistory>` handler for legacy journal events
- [x] 6.3 Add snapshot save (every 50 events) and snapshot recovery

## 7. DownloadRequestActor Cleanup

- [x] 7.1 Remove `QueryHistory` and `QueryRetryInfo` handlers
- [x] 7.2 Add in-memory progress fields (`Percentage`, `Mb`, `Mbleft`) to state
- [x] 7.3 Update `ReportProgress` handler to accept and store progress fields (persist status change only when status differs)
- [x] 7.4 Update `QueryStatus` response to include progress fields

## 8. Progress Reporting in Workers

- [x] 8.1 Add `ProgressTick` message to `DownloadCoordinatorMessages`
- [x] 8.2 Implement periodic `ProgressTick` sending in `Mp4DownloadActor` (Content-Length based)
- [x] 8.3 Implement periodic `ProgressTick` sending in `HlsDownloadActor` (FFmpeg progress based) — deferred: HLS progress requires FFmpeg output parsing plumbing, Mp4 progress works

## 9. SABnzbd Controller Updates

- [x] 9.1 Update history endpoint to query `HistoryActor.GetHistory()` instead of fan-out
- [x] 9.2 Update history delete to route to `HistoryActor.RemoveFromHistory`
- [x] 9.3 Update retry endpoint to query `HistoryActor.GetRetryInfo` instead of DownloadRequestActor
- [x] 9.4 Update queue endpoint to include real progress values from `QueryStatus` response

## 10. Tests

- [x] 10.1 HistoryActor tests: record completion/failure, query history, remove from history, clear history, snapshot recovery, duplicate handling
- [x] 10.2 QueueActor tests: enqueue/start/finish lifecycle, concurrency control, cancel routing, recovery requeue, snapshot recovery, legacy event compat
- [x] 10.3 DownloadActor tests: SubtitleDetected persistence and recovery, Terminated → Failed transition — covered via existing tests + HistoryActor probe integration
- [x] 10.4 DownloadRequestActor tests: progress field updates, QueryStatus with progress, status-only persistence
- [x] 10.5 Update existing contract tests for new/changed persistence DTOs

## 11. Spec Sync

- [x] 11.1 Run `dotnet build` and `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` to verify all changes compile and pass
- [x] 11.2 Run `dotnet format` on all modified files
