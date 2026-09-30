## Context

The DownloadManager is a persistent cluster singleton that manages a queue of downloads with concurrency control. Currently, it also owns time-window scheduling logic: `DispatchNext()` checks `DownloadScheduleHelper.IsWithinSchedule()`, manages a `_scheduleTimer` via `ICancelable`, and depends on `TimeProvider`. This conflation prevents adding manual pause/resume or force-starting individual downloads because the dispatch decision is hardcoded into a single method mixing scheduling and slot management.

The existing `DownloadScheduleHelper` static class is well-tested and handles over-midnight windows correctly — it doesn't need rewriting, only relocating conceptually.

## Goals / Non-Goals

**Goals:**
- Separate scheduling (time-window evaluation) from queue management (slot filling) into distinct actors
- Enable manual pause/resume of the download pipeline via API
- Support force-starting a single download that bypasses both gates
- Persist manual pause state across container restarts
- Expose pipeline status (paused, schedule active, next window) to the UI

**Non-Goals:**
- Pausing/suspending FFmpeg processes mid-download (graceful drain only)
- Per-download scheduling (all downloads share the same schedule)
- Priority queue (downloads remain FIFO)
- Speed limiting / bandwidth throttling

## Decisions

### Decision 1: Dedicated DownloadScheduler actor (not a service or policy object)

An actor (cluster singleton) rather than an `IHostedService` or injected policy, because:
- It needs timer management (`ScheduleTellOnceCancelable`) which is native to Akka actors
- It follows the existing pattern: all coordination in FunkArr is actor-based
- It can be resolved via `Context.GetActor<IDownloadScheduler>()` like other actors
- No persistence needed — on startup it evaluates the current schedule and sends the initial Enable/Disable

**Alternative considered:** Injecting an `ISchedulePolicy` into the DownloadManager. Rejected because timer management still needs to live somewhere, and the Manager would still need to own the timer lifecycle.

### Decision 2: Two-gate dispatch model with independent state

The Manager tracks two independent boolean gates:
- `ScheduleEnabled` — set by DownloadScheduler via `ScheduleEnabled`/`ScheduleDisabled` messages
- `Paused` — set by user via `PauseDownloads`/`ResumeDownloads` messages (persisted)

`DispatchNext()` only proceeds when `!Paused && ScheduleEnabled`. The gates are independent — disabling one doesn't affect the other.

**Why not a single enum (Running/SchedulePaused/ManuallyPaused/Both)?** Because the two concerns are orthogonal. A matrix of states would be fragile. Two booleans compose cleanly.

### Decision 3: ScheduleEnabled defaults to true on recovery

After Manager recovery, `ScheduleEnabled` starts as `true`. The Scheduler sends its initial state shortly after startup. This avoids a startup deadlock where the Manager waits for the Scheduler — in the brief window between Manager recovery and Scheduler's first message, the Manager can dispatch (which is safe: worst case, a download starts slightly outside the window).

**Why not persist ScheduleEnabled?** It's derived state from the current time + config. Persisting it would create stale data after config changes or long downtime.

### Decision 4: Paused state IS persisted

`PauseDownloads` persists a `DownloadsPaused` event; `ResumeDownloads` persists `DownloadsResumed`. After a container restart, if the user had paused downloads, they stay paused. This matches user intent — a deliberate pause shouldn't be reset by a restart.

### Decision 5: ForceStartDownload bypasses both gates for one download

`ForceStartDownload(Guid)` dispatches exactly one download from the queue (or a specific queued download) regardless of Paused or ScheduleEnabled state. It persists a normal `DownloadDispatched` event — no special event type needed. The forced download flows through the normal lifecycle (SlotFree when done).

### Decision 6: ScheduleDisabled carries NextWindow

The Scheduler sends `ScheduleDisabled(DateTimeOffset? NextWindow)` so the Manager can expose the next window time to the UI without needing to query the Scheduler. `NextWindow` is null when no future window can be determined (e.g., schedule removed while disabled — edge case).

### Decision 7: Graceful drain on pause/disable

When Paused or ScheduleDisabled arrives, running downloads are not interrupted. Workers finish naturally and send `SlotFree`. The Manager receives `SlotFree`, removes the download from Dispatched, but doesn't fill the slot. No new worker states, no FFmpeg interruption, no cancel messages.

### Decision 8: Messages in FunkArr.Messages, not actor-internal

`PauseDownloads`, `ResumeDownloads`, `ForceStartDownload` live in `FunkArr.Messages/Download/` because they're sent from the API layer. `ScheduleEnabled`/`ScheduleDisabled` also live there because they cross actor boundaries (Scheduler → Manager), even though they're internal to the Download domain.

## Risks / Trade-offs

**[Startup race] Manager may dispatch before Scheduler sends initial state** → Acceptable: ScheduleEnabled defaults to true, so downloads may start briefly outside the window. The Scheduler sends its state within milliseconds of startup. For a download pipeline this is negligible.

**[Persistence version] New events added to a running system** → No risk: version 0.x, no migration needed. New events (`DownloadsPaused`/`DownloadsResumed`) are simply new entries in the journal. Old journals without them recover to `Paused=false` (the default), which is correct.

**[Schedule config change while paused]** → The Scheduler reacts to `OnChange` independently. It sends Enable/Disable to the Manager regardless of the Manager's Paused state. Both gates remain independent.
