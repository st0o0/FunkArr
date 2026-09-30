## Context

DownloadActor is an event-sourced sharded entity that orchestrates download jobs through a stage machine (Fetching → AcquiringSubtitle → ConvertingSubtitle → Muxing → Done). Each stage spawns a transient child worker actor. The child sends a result message back to the parent and stops itself.

The current error path has three layers:
1. **Worker catch block** — worker catches exceptions, sends `WorkerFailed` to parent, stops itself
2. **Terminated handler** — parent watches child, handles `Terminated` as a fallback if no `WorkerFailed` was received
3. **Supervision** — `OneForOneStrategy(_ => Directive.Stop)` catches anything that escapes the worker's try/catch

In production, downloads get permanently stuck in "Downloading" status. The `_workerFailedReceived` flag, set before `Persist()`, creates a window where both the primary (WorkerFailed → Persist) and secondary (Terminated) paths are disabled if persistence fails. The QueueActor slot is never released.

## Goals / Non-Goals

**Goals:**
- Guarantee that every started download eventually reaches a terminal state (Done, Failed, or Cancelled)
- Release QueueActor slots even when persistence or message delivery fails
- Add a time-bound safety net so no stage can hang indefinitely
- Close the `OperationCanceledException` gap in subtitle workers

**Non-Goals:**
- Retry logic (failed downloads should fail, not auto-retry)
- Configurable timeouts via options/UI (hardcoded is fine for now)
- Changes to the QueueActor itself (it correctly handles `NotifyJobFinished` already)
- Changes to the persistence backend or journal configuration

## Decisions

### Decision 1: Move `_workerFailedReceived` inside the Persist callback

**Current:** Flag is set before `Persist()`, disabling the `Terminated` fallback before the journal write confirms.

**New:** Set the flag inside the `Persist` callback, after the journal write succeeds. This keeps the `Terminated` handler active as a backup during the persistence window.

**Why not a separate flag?** Adding a second flag (e.g., `_persistInFlight`) increases state complexity. Moving the assignment into the callback is the minimal change that fixes the race.

### Decision 2: Add `OnPersistFailure` override

When `Persist` fails, Akka.Persistence stops the actor by default. Without intervention, the QueueActor never receives `NotifyJobFinished` and the slot stays occupied.

The override will:
1. Force-tell QueueActor with `NotifyJobFinished(nzoId, "failed")`
2. Force-tell DownloadRequestActor with `FailDownload(nzoId, reason)`
3. Log the persistence failure
4. Call `base.OnPersistFailure(...)` to let the default stop behavior proceed

This is a fire-and-forget best-effort notification. If the Tells themselves fail (actor system shutting down), the slot leak is acceptable — a system restart recovers all state anyway.

**Why not `OnPersistRejected`?** `OnPersistRejected` is for journal-level rejection (serialization errors). It's a different failure mode. We add it too for completeness, with the same notification logic.

### Decision 3: Per-stage `ReceiveTimeout` as safety net

Each stage behavior sets `Context.SetReceiveTimeout(duration)` on entry. If no message arrives within the timeout, Akka delivers a `ReceiveTimeout` message. The handler kills the worker (if alive) and transitions to Failed.

Timeouts:
| Stage              | Timeout | Rationale                              |
|--------------------|---------|----------------------------------------|
| Fetching           | 30 min  | Large video files over slow connections |
| AcquiringSubtitle  | 5 min   | Small HTTP fetch or HLS subtitle scan  |
| ConvertingSubtitle | 5 min   | Local file conversion                  |
| Muxing             | 15 min  | FFmpeg remux of large files            |

On timeout:
1. `_currentWorker?.Tell(PoisonPill.Instance)` — graceful kill
2. `HandleWorkerFailed(new WorkerFailed(nzoId, FailureKind.Transient, "Stage timed out"))` — reuse existing failure path

The timeout resets on every received message (including `ProgressTick`), so active downloads with progress won't time out. On stage exit (Become to next stage or Completed), the new behavior either sets a new timeout or clears it via `Context.SetReceiveTimeout(null)`.

**Why `ReceiveTimeout` over `Context.System.Scheduler`?** ReceiveTimeout is built into Akka, auto-cancels on message receipt, and doesn't require manual timer management. It's the idiomatic Akka solution for "nothing happened for too long."

**Why not configurable?** These are safety-net timeouts, not business logic. They should be generous enough to never fire during normal operation. Making them configurable adds options surface for zero user benefit at this stage.

### Decision 4: Remove `OperationCanceledException` filter from subtitle workers

`SubtitleDownloadActor` and `SubtitleExtractActor` use `catch (Exception ex) when (ex is not OperationCanceledException)`. If an `OperationCanceledException` fires, no message reaches the parent and the only signal is `Terminated`.

Since the `Terminated` handler already covers this case (and we're strengthening it), the filter isn't dangerous per se. But it's an unnecessary gap — the worker should always tell the parent before stopping, regardless of exception type. Removing the filter makes the contract explicit: **every worker always sends a result message**.

For subtitle workers, `OperationCanceledException` will result in `SubtitleAcquired(found: false)` — treating cancellation as "no subtitle" is the correct degradation.

## Risks / Trade-offs

**[ReceiveTimeout fires during legitimate long downloads]** → Mitigated by generous timeouts and automatic reset on `ProgressTick`. A 2GB video at 1 MB/s takes ~33 min; the 30-min Fetching timeout might be tight. If this becomes an issue, bump to 60 min — but `ProgressTick` resets the timer, so a download making any progress won't time out.

**[OnPersistFailure notifications are best-effort]** → If the actor system is in a bad state, the Tell calls might not deliver. This is acceptable — a full system restart recovers state from the journal. The override handles the common case (transient SQLite lock contention) where the system is otherwise healthy.

**[Moving the flag creates a brief window for duplicate failure processing]** → Between `Persist` call and callback, both `WorkerFailed` (already being processed) and `Terminated` could theoretically fire. But `Persist` stashes incoming messages during processing, so `Terminated` won't be handled until after the callback completes and `_workerFailedReceived` is set. No race exists.

**[Removing OCE filter changes subtitle error behavior]** → Previously, `OperationCanceledException` would cause `Terminated` (which triggers `HandleWorkerFailed` with `FailureKind.Transient`). Now it sends `SubtitleAcquired(found: false)`, which skips subtitles gracefully. This is strictly better behavior — a cancelled subtitle fetch shouldn't fail the entire download.
