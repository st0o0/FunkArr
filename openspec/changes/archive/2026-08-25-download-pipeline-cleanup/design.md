## Context

The download pipeline has three persistent actors with overlapping responsibilities. QueueActor manages scheduling but also tracks completed job IDs and handles history removal. DownloadRequestActor (sharded, per nzoId) serves both live-status queries for the queue API and post-completion queries for the history/retry APIs — requiring fan-out across all completed entities on every history request. DownloadActor has correctness issues: `HasSubtitle` is set in-memory without a persisted event, and unhandled `Terminated` messages from crashed workers leave downloads stuck. No actor uses snapshots, and byte-level progress reporting is missing.

## Goals / Non-Goals

**Goals:**
- Clear single-responsibility separation: QueueActor schedules, DownloadRequestActor tracks live status, HistoryActor owns post-completion data
- Eliminate the N-way fan-out for history queries
- Fix event-sourcing correctness (HasSubtitle, Terminated handling)
- Add byte-level progress for SABnzbd queue API
- Add snapshot support for QueueActor and HistoryActor
- Remove dead code artifacts
- Test coverage for QueueActor and HistoryActor

**Non-Goals:**
- Download resume/checkpointing for partially completed stages
- Snapshot support for DownloadActor or DownloadRequestActor (low event volume per entity)
- Worker name collision guard (mitigated by Directive.Stop, edge case only)
- Passivation timeout tuning for DownloadRequestActor (future work)

## Decisions

### D1: HistoryActor as singleton persistent actor (not sharded)

History is queried as a collection (list all completed downloads). A singleton actor with all history entries in-memory eliminates fan-out entirely. The dataset is bounded — hundreds to low thousands of entries at most for a personal media server.

**Alternative considered:** Sharded HistoryActor per nzoId — same fan-out problem as today, no benefit. Rejected.

**Alternative considered:** Database query instead of actor — would bypass the actor model and introduce a second query path. The journal already stores everything needed. Rejected.

### D2: HistoryActor stores complete history entries, not just IDs

Unlike QueueActor which only stored nzoId references, HistoryActor receives and persists the full completion/failure record (title, category, outputPath, completedAt, errorMessage, retryInfo). This makes the actor self-contained — no secondary lookups needed.

**Messages:**
- `RecordCompletion(nzoId, title, category, outputPath, downloadUrl, subtitleUrl, completedAt)` — from DownloadActor on success
- `RecordFailure(nzoId, title, category, error, downloadUrl, subtitleUrl, failedAt)` — from DownloadActor on failure
- `GetHistory()` → replies with `HistoryResponse(List<HistoryEntry>)`
- `GetRetryInfo(nzoId)` → replies with `RetryInfo(nzoId, downloadUrl, title, subtitleUrl, category, status)` or `RetryNotFound`
- `RemoveFromHistory(nzoId)` — removes entry
- `ClearHistory()` — removes all entries

### D3: DownloadActor notifies HistoryActor directly

DownloadActor already notifies QueueActor on completion. Adding a second tell to HistoryActor is simple and keeps the notification path explicit. HistoryActor is resolved from `IActorRegistry` (singleton).

**Flow change:**
```
Before:  DownloadActor → QueueActor (NotifyJobFinished)
                        → DownloadRequestActor (CompleteDownload)

After:   DownloadActor → QueueActor (NotifyJobFinished)
                        → DownloadRequestActor (CompleteDownload)
                        → HistoryActor (RecordCompletion)
```

### D4: SABnzbd controller routes history/retry to HistoryActor

The controller changes from fan-out pattern to single ask:
```
Before:  QueueActor.GetCompletedJobIds → fan-out N × DownloadRequestActor.QueryHistory
After:   HistoryActor.GetHistory (single ask, returns all entries)
```

Retry similarly changes from DownloadRequestActor.QueryRetryInfo to HistoryActor.GetRetryInfo.

History delete changes from QueueActor.RemoveFromHistory to HistoryActor.RemoveFromHistory.

### D5: SubtitleDetected event for HasSubtitle

Add a new domain event `SubtitleDetected(nzoId, found)` persisted when the coordinator receives `SubtitleAcquired`. Recovery replays this to restore `_hasSubtitle`. The persistence DTO is `DcSubtitleDetected` with fields `[JsonProperty("nzo")]` and `[JsonProperty("f")] bool Found`.

### D6: Terminated → Failed transition

In DownloadActor, when `Terminated` is received for a child worker and no `WorkerFailed` was received in the current stage, treat it as an unexpected crash: persist `JobFailed(FailureKind.Transient, "Worker terminated unexpectedly")`, notify tracker and queue, transition to Completed. Track whether `WorkerFailed` was already received via a `_workerFailedReceived` flag reset on each stage entry.

### D7: Progress reporting — in-memory only

Worker actors send `ProgressTick(downloadedBytes, totalBytes)` to parent DownloadActor. DownloadActor forwards `ReportProgress(nzoId, status, percentage, mb, mbleft)` to DownloadRequestActor. Progress data is stored in DownloadRequestActorState as in-memory fields (not persisted — ephemeral data that resets on recovery). `QueryStatus` response gains `Percentage`, `Mb`, `Mbleft` fields.

**Progress source in workers:**
- `Mp4DownloadActor`: HttpClient with progress-reporting stream wrapper (Content-Length header for total, bytes-read counter for downloaded)
- `HlsDownloadActor`: FFmpeg progress output parsed by `FfmpegProgressParser` (duration-based percentage)

**Tick interval:** Every 2 seconds or 1MB, whichever comes first. Controlled by worker actors.

### D8: Snapshot support for QueueActor and HistoryActor

Both actors save a snapshot every 50 events (configurable). On recovery, load latest snapshot, then replay only subsequent events.

**QueueActor snapshot:** `QueueSnapshot { Queue: List<QueueEntry>, Active: HashSet<string> }`
**HistoryActor snapshot:** `HistorySnapshot { Entries: List<HistoryEntry> }`

Snapshot DTOs live in `FunkArr.Persistence/` alongside journal DTOs.

### D9: Dead code removal

- Delete `DownloadJob.cs` (unused `DownloadJob` record and `DownloadStatus` enum)
- Delete `DownloadOutcome.cs` (unused `Success`/`Failure` discriminated union)
- Delete `DownloadProgress.cs` (unused thread-safe progress tracker — progress is now in-memory on DownloadRequestActorState)
- `DcJobAccepted` DTO: keep `[JsonProperty("tmp")]` and `[JsonProperty("out")]` for deserialization backward compatibility, but `ToJournal()` writes empty strings and `ToDomain()` ignores them (already the case, just documenting)

## Risks / Trade-offs

**[HistoryActor memory growth]** → All history entries live in memory. Mitigation: a personal media server produces maybe 10-50 downloads/month. Even after years, this is < 10K entries. Snapshot serialization keeps recovery fast.

**[Breaking QueueActor journal]** → Removing `JobRemovedFromHistory` event means old journals with this event will fail recovery. Mitigation: v0.x allows breaking changes. Add a `Recover<QueueJobRemovedFromHistory>` handler that silently ignores the event during a transition period, then remove it later.

**[Progress accuracy]** → HLS progress is estimated from FFmpeg duration output, not exact byte counts. Mitigation: acceptable for UI display purposes. Mp4 progress is accurate when Content-Length is available.

**[Dual notification on completion]** → DownloadActor now tells three actors (Queue, Tracker, History) on completion. If one tell is lost (unlikely on same node), data diverges. Mitigation: single-node deployment makes message loss extremely unlikely. History can be reconstructed from journal replay if needed.
