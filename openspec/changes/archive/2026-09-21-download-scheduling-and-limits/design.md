## Context

DownloadManager is the single dispatch point - `DispatchNext()` is called after enqueue, slot-free, retry, and recovery. It loops while `Dispatched.Count < maxConcurrent`, picking queued items and sending `StartDownload` to the worker shard region. Workers run FFmpeg via `FfmpegRunner` (using FFMpegCore library) and are unaware of scheduling.

Currently there is no time-based or bandwidth-based control. All new downloads dispatch immediately when slots are available.

## Goals / Non-Goals

**Goals:**
- Allow restricting new download dispatches to configured time windows (server-local time)
- Allow capping download speed via FFmpeg protocol options
- All new behavior opt-in - no config change = identical behavior to today
- Clean timer-based scheduling - no polling loops

**Non-Goals:**
- Pause/resume of in-progress downloads (workers always run to completion)
- Volume caps (daily/monthly byte limits) - separate future change
- Per-category schedules or speed limits - single global config
- Timezone configuration field - server-local time via Docker TZ env var
- UI for editing schedule (read-only display first, edit can come later)

## Decisions

### 1. Time-window check in DispatchNext

**Decision:** Add an `IsWithinSchedule()` check at the top of `DispatchNext()`. When a schedule is configured and current time is outside all windows, skip dispatching and schedule a timer to call `DispatchNext()` at the next window start.

**Why:** Single point of change. Workers, persistence, and queue state are untouched. The timer approach (via `Context.System.Scheduler.ScheduleTellOnce`) is idiomatic Akka and avoids polling.

**Alternative considered:** Separate SchedulerActor that gates the Manager. Rejected - adds actor coordination complexity for a simple clock check. The Manager already owns dispatch decisions.

### 2. Over-midnight slot logic

**Decision:** A slot with `Start > End` (e.g., 23:00–02:00) means the window wraps midnight. The check becomes `time >= Start || time < End`. Normal slots use `time >= Start && time < End`.

**Why:** This is the most common use case. Making the user split into two slots (23:00–00:00 and 00:00–02:00) is hostile UX.

### 3. Timer management

**Decision:** Store one `ICancelable` timer reference on the Manager. When scheduling a wake-up timer, cancel any existing one first. Timer sends a private `ScheduleWake` message to self, which calls `DispatchNext()`.

**Why:** Prevents timer accumulation. Only one pending wake-up is ever needed (the nearest window start). Cancellation on new enqueue or config change ensures correctness.

**When timers fire:**
- After `DispatchNext()` determines it's outside a window → timer to next window start
- On `IOptionsMonitor<DownloadOptions>.OnChange` → cancel existing timer, re-evaluate immediately

### 4. Speed limit via FFmpeg protocol options

**Decision:** Pass speed limit as an FFmpeg input option via `FromUrlInput` with `-maxrate` on the HTTP input. The FFMpegCore library supports `.WithCustomArgument()` on input options.

**Why:** Protocol-level limiting is the simplest approach - no proxy, no external tooling. FFmpeg respects `-re` and rate options on HTTP inputs. Applied per-worker, so total bandwidth is `SpeedLimitBytesPerSecond × ConcurrentDownloads` at most.

**Alternative considered:** OS-level traffic shaping (tc/iptables). Rejected - not portable, requires elevated privileges, hard to configure in Docker.

### 5. Config model as list of slots

**Decision:** `DownloadSchedule` is `List<DownloadTimeSlot>` where `DownloadTimeSlot` has `Start` and `End` as `TimeOnly`. Empty or null list means unrestricted.

**Why:** Supports the single-window case now. Adding a second slot later requires zero schema changes. `TimeOnly` is the idiomatic .NET type for time-of-day without date.

### 6. IOptionsMonitor for hot-reload

**Decision:** DownloadManager subscribes to `IOptionsMonitor<DownloadOptions>.OnChange` to pick up config changes without restart. On change, update `_maxConcurrent`, cancel any pending schedule timer, and call `DispatchNext()`.

**Why:** Already the pattern used for options in the project. Schedule changes should take effect immediately - users shouldn't need to restart the container to adjust a time window.

## Risks / Trade-offs

**[Risk] Speed limit is per-worker, not global** → With 3 concurrent downloads at 10 MB/s limit each, actual bandwidth is up to 30 MB/s. Mitigation: Document this clearly. A global limit would require coordination between workers, which is significantly more complex. For the residential use case, per-worker limits are sufficient.

**[Risk] Timer drift on long sleep** → If the next window is 20 hours away, the timer fires after 20h. System clock changes (DST, NTP jumps) are not re-evaluated. Mitigation: Acceptable for v0.x. A periodic re-check (e.g., every hour) could be added later if needed.

**[Risk] FFMpegCore library may not expose all input-level options** → `.WithCustomArgument()` is available but input-level protocol options need verification. Mitigation: Spike task to verify FFmpeg rate limiting works with the library's API before implementing.

**[Risk] Config validation edge cases** → `Start == End` (zero-length window), overlapping slots, or slots covering 24h. Mitigation: Validate in options validation - `Start == End` is rejected, overlapping is allowed (union semantics).
