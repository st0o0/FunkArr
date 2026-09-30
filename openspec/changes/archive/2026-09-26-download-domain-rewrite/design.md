## Context

The Download domain has two core actors that need work:

**DownloadWorker** (sharded, persistent): Runs FFmpeg to download and remux media. Currently loses route info on recovery, has no retry capability, no phase tracking, uses `DateTimeOffset.UtcNow` directly, and duplicates `RecordDownload` construction at three call sites.

**DownloadHistoryManager** (singleton, persistent): Holds all download history records in an unbounded in-memory list. No trimming, no indexing, recomputes stats on every query.

FunkArr is the download client itself (SABnzbd-compatible API). Sonarr/Radarr handle retries by blacklisting and searching for alternative releases, which is meaningless for single-source Mediathek streams. Transient retry must happen at the Worker level.

## Goals / Non-Goals

**Goals:**
- DownloadWorker persists all state needed for recovery (including route info)
- Explicit phase state machine replaces implicit status guards
- Configurable auto-retry with exponential backoff for transient failures (default off)
- Failure classification (transient vs permanent) drives retry decisions
- TimeProvider used consistently
- DownloadHistoryManager bounded by MaxRecords with FIFO trim
- Pre-aggregated stats eliminate per-query recomputation
- Dictionary index for O(1) Contains checks

**Non-Goals:**
- FFmpeg resume (partial download continuation) -- would require FFmpeg `-ss` seeking and segment tracking, out of scope
- Moving DownloadHistoryManager to FunkArr.History -- they are semantically different (download history vs scoring history)
- SABnzbd `mode=retry` implementation changes -- verify existing mapping works, flag if broken
- Changing the Manager's queue/dispatch logic
- UI changes beyond consuming new Phase/Attempt fields

## Decisions

### 1. Phase state machine via state enum, not Become

**Decision**: Model phases as a `DownloadPhase` enum in the state record. Do not use Akka Become/Unbecome.

**Rationale**: The Worker's message set doesn't change between phases (it always handles Cancel, QueryStatus, Progress). Become would add complexity for no benefit. The phase tracks where in the pipeline the Worker is, not which messages it accepts.

**Phases**: `Initialized` | `SubtitleDownload` | `VideoDownload` | `Remuxing` | `Moving` | `Completed` | `Failed`

Phase transitions are persisted via `DownloadPhaseChanged` events. Progress-related fields remain transient.

### 2. Retry via IWithTimers scheduled self-message

**Decision**: Use `IWithTimers` to schedule a `RetryAttempt` self-message after backoff delay. Not a supervisor strategy, not an external retry actor.

**Rationale**: Retry is per-download state, not supervision. The Worker knows its attempt count, failure kind, and backoff. A timer-based self-message keeps retry logic inside the Worker where it belongs. The timer survives in-memory but not crashes -- which is fine: on recovery the Worker resets to Initialized and the Manager re-dispatches.

**Backoff formula**: `min(RetryBackoffBase * 2^(attempt-1), 5 minutes)`

### 3. Failure classification at FFmpeg error parsing level

**Decision**: Extend `FfmpegRunner.ExtractError` to also return a `FailureKind`. Classify based on error string patterns.

**Transient**: HTTP 503, timeout, connection reset, "Server returned 5xx", network unreachable
**Permanent**: HTTP 404, HTTP 403, "No such file", empty URL, invalid format

**Rationale**: The FFmpeg error output is the only source of truth for why a download failed. Classification at parse time avoids pattern-matching in the actor.

### 4. RecordDownload constructed once via state extension method

**Decision**: Add `DownloadWorkerState.ToRecordDownload(TimeProvider, DataPaths.ResolvedDownload?)` extension method. All three call sites (empty URL, success, failure) call this single method.

**Rationale**: Currently the same `RecordDownload` message is manually constructed at three places with slightly different parameters. A single state-based factory eliminates duplication and ensures consistency.

### 5. DownloadHistoryManager trimming at persist time

**Decision**: After applying a `HistoryRecorded` event, check if `Records.Count > MaxHistoryRecords`. If so, remove oldest records (FIFO) until within limit. Persist a `HistoryTrimmed(int Count)` event for recovery consistency.

**Alternative considered**: Trim only at snapshot time. Rejected because in-memory state would grow unbounded between snapshots, and recovery would replay events that get trimmed anyway.

### 6. Pre-aggregated stats as running counters

**Decision**: Add `HistoryStats` record to DownloadHistoryManagerState: `TotalCompleted`, `TotalFailed`, `TotalBytes`, `TotalDownloadTimeSeconds`. Updated inline when records are added or removed. `AverageDownloadTimeSeconds` and `SuccessRate` computed from these counters on query.

**Rationale**: Stats queries iterate the full list today. Running counters make queries O(1) at the cost of a few extra fields in the state record.

### 7. Persistence event compatibility

**Decision**: All new events are additive. Updated events (`DownloadInitialized`, `DownloadFaulted`) gain new fields with sensible defaults in the persistence DTO constructor. Existing journals remain readable without migration.

**Rationale**: Version 0.x -- no migration code needed per project conventions. New fields: `RouteName` defaults to `"Direct"`, `ProxyUrl` defaults to null, `FailureKind` defaults to `Permanent` (safe default: don't retry unknown old failures).

### 8. Updated FfmpegResult carries FailureKind

**Decision**: `FfmpegResult` record gains a `FailureKind` field. `FfmpegRunner` populates it during error classification. The Worker uses it directly without re-parsing errors.

## Risks / Trade-offs

- **[Timer-based retry lost on crash]** If the Worker crashes mid-backoff, the retry timer is lost. On recovery the Worker resets to Initialized, the Manager sees a dispatched slot and re-dispatches on next DispatchNext() after recovery reset. The download restarts from scratch, but it does not get stuck. This is acceptable.

- **[Transient classification false positives]** An error classified as transient might actually be permanent (e.g., 503 from a decommissioned endpoint). Mitigated by MaxRetries cap (default 3). After exhausting retries, the download fails and Sonarr/Radarr sees it.

- **[History trim loses old records]** FIFO trim means old successful downloads disappear from history. Acceptable because history is a UI convenience, not an audit log. The trim size (default 1000) is configurable.

- **[Phase granularity depends on Remuxer changes]** SubtitleDownload and VideoDownload phases require the Remuxer to report phase transitions back to the Worker. Currently Remuxer is a single async call. Either split Remuxer into phases or have Worker infer phase from progress data (bytes vs time). Decision: infer from progress data initially (matching existing `DownloadPhase.DerivePhase` logic), add explicit Remuxer phase callbacks later if needed.

## Open Questions

- Should the DownloadScheduler cancel in-progress retry backoff timers when schedule window closes? Current thinking: no -- if a download is in retry backoff and the schedule closes, let the timer fire but have the Worker check schedule state before retrying.
