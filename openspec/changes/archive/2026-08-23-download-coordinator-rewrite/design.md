## Context

The download pipeline currently uses a single Akka.Streams graph inside `DownloadPipelineActor`. All downloads share one stream — failure in one job can kill the stream for all, cancellation requires re-materialization, and per-job retry/pause is impossible. Phase 2c replaces this with a `DownloadCoordinator` shard entity (one per nzoId) that spawns transient child workers per stage.

The existing services (`Mp4DownloadService`, `HlsDownloadService`, `MuxingService`, `SubtitleAcquisitionService`, `SubtitleNormalizer`) remain unchanged — they contain the actual download/mux logic. The stage workers are thin actor wrappers around these services.

## Goals / Non-Goals

**Goals:**
- DownloadCoordinator as a second ShardRegion entity with event-sourced stage machine
- Five transient child worker actors wrapping existing services
- FailureKind classification (Gone/Transient/Malformed/LocalIo)
- QueueCoordinator starts downloads via DownloadCoordinator shard (not DownloadQueueActor)
- Delete DownloadQueueActor, DownloadPipelineActor, stream stage files

**Non-Goals:**
- Akka.Streams-based DirectDownloadWorker with KillSwitch (deferred — use service directly first, add Streams later for bandwidth throttle/progress)
- Retry logic (deferred — DownloadCoordinator fails immediately, retry can be added later)
- Pause/Resume on LocalIo (deferred — requires QueueCoordinator changes)

## Decisions

### D1: Stage workers use ReceiveAsync wrapping existing services

**Decision:** Each worker uses `ReceiveAsync` to call the existing service, then tells the parent the result and stops itself. Services remain unchanged.

**Why:** The services already handle HTTP, FFmpeg, file I/O. The actor wrapper adds: isolation, supervision, and clean lifecycle (spawn → work → report → stop).

### D2: DownloadCoordinator stage machine via Become()

**Decision:** The coordinator uses `Become(Fetching)`, `Become(AcquiringSubtitle)`, etc. to transition between stages. Each stage handler spawns a child worker and watches it.

**Why:** Become() is simpler than a state variable + switch. Each stage has its own message handlers. Cancel is always available via a common handler.

### D3: Clean cut — no migration from old journal

**Decision:** The old `"download-queue"` persistence journal is abandoned. Active downloads at migration time are lost. QueueCoordinator's journal tracks what needs to (re-)start.

**Why:** The event schemas are completely different. Migration code would be complex and one-time-use. In a homelab, losing a few in-progress downloads at upgrade is acceptable.

### D4: DownloadCoordinator stores payload in state (not re-fetching)

**Decision:** The `JobAccepted` event stores the full download payload (videoUrl, subtitleUrl, title, tempPath, outputDir). All subsequent stages use this stored state.

**Why:** Self-sufficient — no callbacks to QueueCoordinator or controllers after initial acceptance.

### D5: Worker result messages include nzoId for routing

**Decision:** Worker completion messages (`VideoFetchDone`, `SubtitleDone`, etc.) include the nzoId. Since the coordinator IS the entity for that nzoId, it doesn't strictly need it, but it aids logging and debugging.

## Risks / Trade-offs

**[Risk] No retry in first implementation** — A transient failure immediately fails the job.
→ Mitigation: Acceptable for v1. Retry logic is additive (check retry count before failing).

**[Risk] Active downloads lost on upgrade** — Old DownloadQueueActor journal is abandoned.
→ Mitigation: QueueCoordinator recovers the queue order. Jobs re-enter as "queued" and restart.
