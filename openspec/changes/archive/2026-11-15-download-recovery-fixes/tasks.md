## 1. Persistence Schema Extension

- [x] 1.1 Add optional `Category` field (`PersistedMediaType?`) to `DownloadEnqueued` in `FunkArr.Persistence/Events/Download/`. Use `[JsonProperty]` with a new property name. Verify existing Verify snapshot still passes (extend-only).
- [x] 1.2 Add `Cancelled` value with explicit int to `PersistedDownloadStatus` enum in `FunkArr.Persistence`. Add Verify snapshot test for the new enum shape.
- [x] 1.3 Update `DownloadManagerState.Apply(DownloadEnqueued)` to use `evt.Category` when present (keep `?? PersistedMediaType.Show` fallback for backward compat).

## 2. Slot Leak Fix (DownloadWorker Recovery)

- [x] 2.1 In `DownloadWorker.HandleStart`: when `_state.IsInitialized && _state.Status == WorkerStatus.Failed`, persist `DownloadInitialized` to reset state, then proceed with normal start flow.
- [x] 2.2 In `DownloadWorker.HandleStart`: when `_state.IsInitialized && _state.Status == WorkerStatus.Completed`, send `SlotFree` to the manager and return.
- [x] 2.3 Add test: Worker recovers in Failed status, receives StartDownload, resets and starts download (slot not leaked).
- [x] 2.4 Add test: Worker recovers in Completed status, receives StartDownload, sends SlotFree (slot released).

## 3. Retry Off-by-One Fix

- [x] 3.1 In `DownloadWorker.ShouldRetry`: change `_state.Attempt < opts.MaxRetries` to `_state.Attempt <= opts.MaxRetries`.
- [x] 3.2 Add test: With MaxRetries=3 and RetryEnabled=true, verify 4 total attempts (1 original + 3 retries).
- [x] 3.3 Add test: With MaxRetries=3, the 5th attempt (Attempt=4) is NOT retried.

## 4. Retry Category Fix

- [x] 4.1 In `DownloadManager.HandleRetry`: look up the download's category from `DownloadHistoryManager` state (the history entry has a Category field) and pass it to `DownloadEnqueued`.
- [x] 4.2 Update `DownloadManager.HandleRetry` to include the category in `new DownloadEnqueued(cmd.DownloadId, priority, category)`.
- [x] 4.3 Add test: Retry a failed Movie download — queue entry shows category Movie, not Show.
- [x] 4.4 Add test: Retry a failed Show download — category remains Show.

## 5. ScheduleEnabled Default Fix

- [x] 5.1 In `DownloadManagerState`: change `ScheduleEnabled` default from `true` to `false`.
- [x] 5.2 Add test: After recovery, DispatchNext does NOT dispatch until ScheduleEnabled is received.
- [x] 5.3 Verify existing `DownloadManagerScheduleGateTests` still pass (may need adjustment for new default).

## 6. HasSubtitles Fix

- [x] 6.1 Add `SubtitleMuxed` boolean to `DownloadWorkerState`, defaulting to false.
- [x] 6.2 Thread subtitle mux outcome from `Remuxer` through `FfmpegResult` or a separate field: when Remuxer includes a subtitle track, set `SubtitleMuxed = true` in the worker state.
- [x] 6.3 Change `WorkerStatusResult` construction to use `_state.SubtitleMuxed` instead of `_state.Media.SubtitleUrl is not null`.
- [x] 6.4 Add test: SubtitleUrl present but subtitle download fails — HasSubtitles is false.
- [x] 6.5 Add test: SubtitleUrl present and subtitle download succeeds — HasSubtitles is true.
- [x] 6.6 Add test: No SubtitleUrl — HasSubtitles is false.

## 7. Cancelled History Status

- [x] 7.1 Add `Cancelled` to `DownloadStatus` enum in `FunkArr.Messages.Download` and mapping to/from `PersistedDownloadStatus.Cancelled`.
- [x] 7.2 In `DownloadWorker`: detect cancellation in the FfmpegResult handler (result message "Cancelled") and use Cancelled status in `RecordDownload`.
- [x] 7.3 Update `DownloadHistoryManagerState` to handle Cancelled status in history recording and queries.
- [x] 7.4 Add test: Delete active download — history shows Cancelled, not Failed.
- [x] 7.5 Add test: Transient failure — history still shows Failed.

## 8. Subtitle Cancellation Fix

- [x] 8.1 In `Remuxer`: wrap the subtitle download call in a try-catch for `OperationCanceledException`. On catch, return an FfmpegResult indicating cancellation rather than letting the exception propagate.
- [x] 8.2 Add test: Cancel during subtitle download — result is cancellation, not Permanent failure.

## 9. Integration and Format

- [x] 9.1 Run `dotnet build src/FunkArr.slnx` — zero errors.
- [x] 9.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes` — zero violations.
- [x] 9.3 Run all Download.Tests — all pass.
- [x] 9.4 Run all existing test projects that touch download (Api.Tests, ArrApi.Tests, IntegrationTests) — all pass.
