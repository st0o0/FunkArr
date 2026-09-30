## 1. Messages & Persistence Events

- [x] 1.1 Add `PauseDownloads` and `ResumeDownloads` message records in `FunkArr.Messages/Download/`
- [x] 1.2 Add `ForceStartDownload(Guid DownloadId)` and `ForceStartDownloadResult(bool Success, string? Error)` message records in `FunkArr.Messages/Download/`
- [x] 1.3 Add `ScheduleEnabled` and `ScheduleDisabled(DateTimeOffset? NextWindow)` message records in `FunkArr.Messages/Download/`
- [x] 1.4 Add `DownloadsPaused` and `DownloadsResumed` persistence event records in `FunkArr.Persistence/Events/Download/`
- [x] 1.5 Add `IDownloadScheduler` marker interface in `FunkArr.Core/ActorKeys.cs`

## 2. DownloadManager Refactor

- [x] 2.1 Extend `DownloadManagerState` with `Paused` (bool), `ScheduleEnabled` (bool), `NextWindow` (DateTimeOffset?) fields and Apply methods for `DownloadsPaused`, `DownloadsResumed`
- [x] 2.2 Remove `TimeProvider` dependency, `_scheduleTimer`, `ScheduleWake` record, `ScheduleWakeUp()`, `CancelScheduleTimer()` from DownloadManager
- [x] 2.3 Simplify `DispatchNext()` to two-gate check: `if (Paused || !ScheduleEnabled) return;`
- [x] 2.4 Add `PauseDownloads` handler: persist `DownloadsPaused` if not already paused
- [x] 2.5 Add `ResumeDownloads` handler: persist `DownloadsResumed` if currently paused, call `DispatchNext()`
- [x] 2.6 Add `ScheduleEnabled` handler: set flag, clear NextWindow, call `DispatchNext()`
- [x] 2.7 Add `ScheduleDisabled` handler: set flag and NextWindow
- [x] 2.8 Add `ForceStartDownload` handler: dispatch single download bypassing both gates
- [x] 2.9 Add recovery handlers for `DownloadsPaused` and `DownloadsResumed` events
- [x] 2.10 Extend `QueueResult` with `IsPaused`, `IsScheduleActive`, `NextWindow` and update `HandleQueryQueue` to populate them
- [x] 2.11 Remove `OnOptionsChanged` schedule-related logic (keep ConcurrentDownloads update)

## 3. DownloadScheduler Actor

- [x] 3.1 Create `DownloadScheduler.cs` in `FunkArr.Download/` — ReceiveActor (not persistent), inject `TimeProvider`, `IOptionsMonitor<DownloadOptions>`, resolve `IDownloadManager`
- [x] 3.2 Implement startup evaluation: check schedule, send initial `ScheduleEnabled`/`ScheduleDisabled` to Manager
- [x] 3.3 Implement timer management for schedule boundary transitions (window open → send Enable + timer for close, window close → send Disable + timer for next open)
- [x] 3.4 Implement `OnChange` handler: cancel timer, re-evaluate schedule
- [x] 3.5 Register `DownloadScheduler` as Cluster Singleton with key `IDownloadScheduler` in `AkkaSetupContainer.cs`

## 4. API Endpoints

- [x] 4.1 Add `POST /api/downloads/pause` endpoint in `DownloadsApiEndpoints`
- [x] 4.2 Add `POST /api/downloads/resume` endpoint in `DownloadsApiEndpoints`
- [x] 4.3 Add `POST /api/downloads/queue/{id}/force-start` endpoint in `DownloadsApiEndpoints`
- [x] 4.4 Update queue snapshot and SSE response models to include `isPaused`, `isScheduleActive`, `nextWindow`

## 5. Tests

- [x] 5.1 Add DownloadManager tests for PauseDownloads/ResumeDownloads (persist, recover, graceful drain)
- [x] 5.2 Add DownloadManager tests for two-gate dispatch (both gates, each gate independently)
- [x] 5.3 Add DownloadManager tests for ForceStartDownload (success, not-queued, already-dispatched)
- [x] 5.4 Add DownloadManager tests for ScheduleEnabled/ScheduleDisabled handling
- [x] 5.5 Add DownloadScheduler tests: startup evaluation, timer transitions, config changes
- [x] 5.6 Update existing DownloadManager tests that depend on TimeProvider/schedule logic
