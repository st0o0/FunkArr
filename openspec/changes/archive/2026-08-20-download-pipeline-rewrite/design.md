## Context

The DownloadQueueActor is the core of FunkArr's download pipeline. It's a `ReceivePersistentActor` with event-sourced state that manages a queue of download jobs. The actor materializes an Akka.Streams pipeline to process downloads concurrently.

The existing specs (`download-pipeline`, `stream-supervision`, `muxing-pipeline`) already describe the correct architecture: a `GraphDSL`-based pipeline with Partition/Merge for HLS vs. MP4 routing, composable stage classes, and recovery that re-pushes queued jobs. However, the current implementation deviates:

- Stage classes exist (`DownloadStages`, `SubtitleStages`, `MuxStages`) but are dead code
- A single `SelectAsyncUnordered` lambda handles everything inline
- Recovery doesn't call `PushQueuedJobs()`
- `Sink.ForEach` is used instead of `Sink.ActorRef`
- Temp files are never cleaned up
- No CancellationToken propagation

## Goals / Non-Goals

**Goals:**
- Wire the existing stage classes into a composed GraphDSL pipeline
- Use `Sink.ActorRef` so stream outcomes flow directly into the actor mailbox
- Fix recovery to re-push queued jobs after stream materialization
- Add temp-file cleanup after muxing (success path)
- Thread CancellationToken through pipeline via KillSwitch-linked CancellationTokenSource
- Remove dead code (`RetryCount`, unused stage assignments)

**Non-Goals:**
- Per-stage parallelism configuration (spec explicitly says shared `ConcurrentDownloads`)
- Retry logic (no requirement exists; `RetryCount` is being removed)
- Changing the persistence model or event DTOs
- Changing external API behavior (SABnzbd/Newznab)

## Decisions

### 1. GraphDSL with Partition/Merge for source type routing

The pipeline uses `GraphDSL.Create` to build a graph that partitions `DownloadRequest` elements by source type (HLS vs. MP4), routes each to its respective download flow, then merges results into the common subtitle → normalize → remux path.

```
Source.Queue<DownloadRequest>(64, Backpressure)
    │
    ▼
KillSwitch.Flow
    │
    ▼
┌───Partition(2, SourceTypeRouter)───┐
│                                     │
│  outlet[0]         outlet[1]        │
│     │                  │            │
│  Mp4Flow            HlsFlow        │
│     │                  │            │
│     └──────Merge───────┘            │
│              │                      │
│        SubtitleAcquire              │
│              │                      │
│        SubtitleNormalize            │
│              │                      │
│           Remux                     │
│              │                      │
│         CleanupFlow                 │
│              │                      │
│       Sink.ActorRef(self)           │
└─────────────────────────────────────┘
```

**Why GraphDSL over linear Via chains**: Partition/Merge requires the graph DSL — you can't express fan-out/fan-in with linear `Flow.Via()` composition. This also matches what the existing `download-pipeline` spec requires.

**Alternative considered**: A single flow with `if/else` inside the lambda (current approach). Rejected because it prevents independent error handling and violates the spec's composability requirement.

### 2. Sink.ActorRef instead of Sink.ForEach

Replace `Sink.ForEach` with `Sink.ActorRef<MuxOutcome>(self, new StreamCompleted())`:

- Stream outcomes (`MuxOutcome.Success`, `MuxOutcome.Failure`, `MuxOutcome.Skipped`) arrive as regular actor messages
- The `StreamCompleted` message is sent automatically when the stream completes normally
- For stream failures, `done.ContinueWith(...).PipeTo(self)` still sends `StreamFailed`

**Why**: `Sink.ForEach` runs a callback outside the actor's message loop, creating a potential race with actor state. `Sink.ActorRef` routes everything through the mailbox, maintaining single-threaded actor semantics. The actor already has `Command<MuxOutcome.Success>` etc. handlers (via the `MuxingCompleted`/`MuxingFailed` events) — the sink just delivers to them directly.

**Alternative considered**: `Sink.ActorRefWithAck`. Rejected — we don't need per-element backpressure acknowledgment from the actor; `Source.Queue` with `OverflowStrategy.Backpressure` already handles input-side backpressure.

### 3. Pipeline outcome message wrapping

The stages currently send events to `self` via `Self.Tell` (e.g., `DownloadStarted`, `DownloadCompleted`). These stay as-is for progress tracking. The pipeline's final output type changes:

- Current: `MuxOutcome` → `Sink.ForEach` callback → `self.Tell(MuxingCompleted/MuxingFailed)`
- New: `MuxOutcome` → `Sink.ActorRef(self)` → actor handles `MuxOutcome` directly

This means adding `Command<MuxOutcome.Success>`, `Command<MuxOutcome.Failure>`, `Command<MuxOutcome.Skipped>` handlers in `Ready()`, which persist the appropriate events and trigger cleanup/metrics.

### 4. CancellationToken via KillSwitch-linked CancellationTokenSource

Create a `CancellationTokenSource` that is cancelled when the KillSwitch shuts down:

```csharp
_cts = new CancellationTokenSource();
_killSwitch = KillSwitches.Shared("download-pipeline");
```

In `PostStop()`: cancel the CTS before shutting down the KillSwitch. Pass `_cts.Token` into stage factories so `DownloadAsync`, `MuxAsync` etc. can observe cancellation.

**Why not just rely on the KillSwitch**: The KillSwitch cancels the stream graph (no more elements flow), but in-flight `SelectAsyncUnordered` lambdas continue running their async operations to completion. A CancellationToken lets those operations abort early.

### 5. Temp-file cleanup as a post-mux stage

Add a `CleanupStages.CleanupTempFiles` flow after the remux stage. On `MuxOutcome.Success`, delete the source video and intermediate subtitle files from `TempPath`. On `MuxOutcome.Failure`, preserve files for debugging (per muxing-pipeline spec).

This is a thin `Select` (not async) that calls `IFileService.DeleteIfExists` and passes the `MuxOutcome` through unchanged.

### 6. Recovery fix

In `OnRecoveryCompleted()`, add `PushQueuedJobs()` call after `Materializing()`. The method `Materializing()` already calls `MaterializeStream()`, `Become(Ready)`, and `Stash.UnstashAll()`. Adding `PushQueuedJobs()` at the end of `OnRecoveryCompleted()` (after `Materializing()`) ensures recovered queued jobs are offered to the newly materialized stream.

### 7. SourceTypeRouter as a static partition function

`DownloadSourceDetector.Detect` already exists and returns `DownloadSourceType`. The partition function maps this to outlet index: `HLS → 0`, `Direct → 1`. This keeps the routing logic in one place.

## Risks / Trade-offs

**[Risk] GraphDSL is more complex to read than a linear flow** → The graph is documented with an ASCII diagram in both the design and as a code comment. Each stage is a named variable. The actual logic per stage is simpler because each does one thing.

**[Risk] Sink.ActorRef drops messages if actor mailbox is full** → In practice, the actor mailbox is unbounded (Akka default). The stream's own backpressure (Source.Queue buffer of 64) is the real throttle. Not a concern.

**[Risk] CancellationToken adds complexity to stage signatures** → Each stage factory gains one parameter. The services (`DownloadService`, `HlsDownloadService`, `MuxingService`) already accept `CancellationToken` in their async methods — we're just threading it through.

**[Risk] Temp cleanup on success only leaves orphans on failure** → This matches the muxing-pipeline spec ("preserved for debugging"). A periodic cleanup of stale temp files could be added later but is a non-goal for this change.
