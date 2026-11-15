## Context

The download pipeline has 7 bugs discovered during a comprehensive code review. They cluster around three themes: (1) actor recovery after crashes, (2) retry semantics, and (3) incorrect status/flag reporting. All bugs are in the Download domain (`FunkArr.Download`), with minor touches to `FunkArr.Persistence` (extend-only event field) and `FunkArr.Messages` (new enum value).

Current state:
- DownloadWorker is a persistent sharded entity. HandleStart guards on `Status == Initialized` and silently drops StartDownload otherwise. After recovery from a Failed phase (retry-wait crash), the manager re-dispatches but the worker ignores the StartDownload, leaking the slot permanently.
- `ShouldRetry` uses `Attempt < MaxRetries`. With 1-based attempts, MaxRetries=3 allows attempts 1,2,3 (original + 2 retries). The option name suggests 3 retries.
- Manager HandleRetry persists `DownloadEnqueued(id, priority)` without a category field. State Apply defaults null category to Show. Movies retried via UI/API lose their category.
- ScheduleEnabled defaults to true in DownloadManagerState and is not persisted. On recovery, DispatchNext fires immediately — before the scheduler can send ScheduleDisabled.
- WorkerStatusResult.HasSubtitles is derived from `SubtitleUrl is not null`, not from whether the subtitle was actually downloaded and muxed.
- Delete sends CancelDownload. The resulting FfmpegResult cancellation flows through the permanent-failure path, recording a Failed history entry. There is no Cancelled status.
- Subtitle download cancellation (OperationCanceledException) escapes Remuxer, becomes a Permanent FfmpegResult, and is never retried. This is correct behavior (cancel should not retry) but the classification as "Permanent failure" rather than "Cancelled" is misleading in history.

## Goals / Non-Goals

**Goals:**
- Fix all 7 bugs with minimal persistence schema changes
- Maintain backward compatibility with existing persisted events (extend-only)
- Add targeted tests for each fix

**Non-Goals:**
- Redesigning the download pipeline or worker lifecycle
- Adding snapshots to DownloadWorker (separate concern)
- Changing the retry backoff formula or FFmpeg failure classification
- Fixing the stale download-retry spec (describes FfmpegRunner that no longer exists)

## Decisions

### D1: Slot leak fix — reset worker on re-dispatch

**Decision:** In DownloadWorker.HandleStart, when `_state.IsInitialized && _state.Status == WorkerStatus.Failed`, persist a DownloadInitialized event (same as HandleReset) to reset to Initialized, then proceed with the normal start flow.

**Why not send SlotFree instead?** Releasing the slot would leave the download stuck in Failed with no way to restart except manual retry. The manager already decided to re-dispatch this download (via ResetDispatched), so the correct behavior is to honor that intent by resetting and starting.

**Why not fix in the manager?** The manager's ResetDispatched correctly moves items from Dispatched to Queued. The problem is that the worker was in retry-wait when it crashed, so its recovered state is Failed. The fix belongs in the worker because it's the worker that knows its own phase.

### D2: Retry off-by-one — change to `<=`

**Decision:** Change `_state.Attempt < opts.MaxRetries` to `_state.Attempt <= opts.MaxRetries`.

This makes MaxRetries=3 mean "3 retries after the original attempt" (4 total attempts). The first attempt is 1, so retries happen at attempts 2, 3, 4.

**Alternative considered:** Changing Attempt to 0-based. Rejected because the existing spec says "The first attempt is 1" and persisted events already contain 1-based attempt numbers. Changing the semantic would break existing persistence state.

### D3: Retry category — extend DownloadEnqueued event

**Decision:** Add an optional `Category` field (type `PersistedMediaType?`) to the `DownloadEnqueued` persistence DTO. The manager's HandleRetry queries the worker for its current category before persisting DownloadEnqueued.

**Why not query DownloadHistoryManager?** The worker already has the category in its persisted state and is addressable by ID. The history manager would require a roundtrip for data the worker already holds. However, the worker might be passivated. So instead: the category is stored in the DownloadHistoryManager's record for this download. HandleRetry already interacts with the history manager (RemoveHistoryEntry after persist). The simplest approach: look up the category from the history entry before persisting.

**Revised decision:** Store the category on the DownloadManagerState QueueEntry directly. When a download is first enqueued (via AddDownload), the category is already available in the AddDownload command. Persist it in DownloadEnqueued. On retry, read it from the history entry's category field before enqueueing. This avoids an Ask to the worker.

**Persistence compatibility:** Adding an optional field to DownloadEnqueued is extend-only and backward compatible. Existing events without the field will deserialize with Category=null, which defaults to Show — same as current behavior.

### D4: ScheduleEnabled default — false

**Decision:** Change `ScheduleEnabled` default in DownloadManagerState from `true` to `false`. The scheduler sends ScheduleEnabled on startup (within milliseconds). The brief window where dispatch is blocked is acceptable — it only affects the recovery path, and downloads that were in-flight before the crash are re-queued anyway.

**Why not persist ScheduleEnabled?** It's derived state from the scheduler's evaluation of current time vs config. Persisting it would create a stale value (schedule could have changed between crash and restart). The scheduler re-evaluates on startup, so the fresh signal is authoritative.

### D5: HasSubtitles — track actual mux result

**Decision:** Add a `SubtitleMuxed` boolean to DownloadWorkerState, set to true only when Remuxer reports that a subtitle track was included in the output. WorkerStatusResult.HasSubtitles reads from this field instead of `SubtitleUrl is not null`.

The Remuxer already knows whether it included a subtitle (SubtitleDownloader returns Success/Failed). Thread the result through: Remuxer returns whether subtitle was included in its result, worker state records it.

**Persistence:** This is in-memory state derived from the remux result, not a new persistence event. On recovery, SubtitleMuxed defaults to false (conservative — subtitle download is not re-run on recovery). The status is only meaningful for active/completed downloads.

### D6: Cancelled vs Failed history — new DownloadStatus value

**Decision:** Add `Cancelled` to the `PersistedDownloadStatus` enum in FunkArr.Persistence with an explicit int value. When a download is deleted (CancelDownload), the history entry records status Cancelled instead of Failed.

The worker's HandleCancel already passivates without sending RecordDownload. The delete path in the manager sends CancelDownload to the worker but the cancel-FfmpegResult-SlotFree-RecordDownload chain records as Failed. Fix: in the FfmpegResult handler, detect cancellation (the result message already says "Cancelled") and use the Cancelled status.

### D7: Subtitle cancellation — catch in Remuxer

**Decision:** In Remuxer, catch `OperationCanceledException` from the subtitle download path and return a cancellation FfmpegResult rather than letting it propagate as a Permanent failure. The existing `FfmpegResult(false, -1, "Cancelled")` pattern applies.

This is already the behavior for FFmpeg-level cancellation. The fix is to apply the same pattern to the subtitle download phase.

## Risks / Trade-offs

- **[Risk] Extending DownloadEnqueued breaks existing tests** → Mitigation: The field is optional with null default. Existing test data needs no changes. New tests verify the category roundtrip.
- **[Risk] ScheduleEnabled=false could delay first dispatch by seconds** → Mitigation: The scheduler starts and sends ScheduleEnabled within milliseconds. The delay is imperceptible. This is the safe default — better than accidentally dispatching during a restricted window.
- **[Risk] SubtitleMuxed defaults to false on recovery** → Mitigation: This is conservative. A recovered download that had subtitles but crashed will show HasSubtitles=false. This is better than lying (showing HasSubtitles=true when the subtitle failed). The field is only visible in the queue status, not in history (history has its own recording).
- **[Risk] Cancelled enum value in persistence** → Mitigation: Extend-only with explicit int value. Existing entries stay as-is. The Cancelled value only applies to future deletes.
