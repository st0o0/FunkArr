## Context

FunkArr's download execution currently uses a worker-per-download actor model: `DownloadQueueActor` spawns `DownloadWorkerActor` children (one per active download) and sends completed files to a single `MuxingActor`. This works at low concurrency (default 3) but introduces unnecessary overhead at scale — actor lifecycle management, supervision strategies per worker, and sequential muxing bottleneck. The matching pipeline has all building blocks (`MatchingPipeline`, `DateMatcher`, `TvdbClient`) but they are not composed into a coherent flow, and the API endpoints remain stubbed.

The reference project Njord demonstrates that Akka.Streams inside actors is a proven pattern for this codebase: `Source.Queue` as actor→stream bridge, `Self.Tell` as stream→actor bridge, `SharedKillSwitch` for lifecycle, and `StreamSupervision` for resilience.

## Goals / Non-Goals

**Goals:**
- Replace worker-per-download with an Akka.Streams pipeline that supports 20+ concurrent downloads with per-stage parallelism control
- Compose the matching pipeline as testable pure functions
- Wire all stubbed API endpoints to complete the end-to-end flow
- Maintain event-sourced persistence for the download queue

**Non-Goals:**
- CDN rate limiting / throttling (add later if needed)
- HTTP range-resume for interrupted downloads (start from beginning on retry)
- MergeHub/BroadcastHub/StreamRefs topology (single producer, not needed)
- Download scheduling / priority system
- TVDB client implementation (client exists, wiring is in scope, but full implementation of lookup logic deferred if API key infrastructure is not ready)

## Decisions

### Decision 1: Akka.Streams pipeline inside DownloadQueueActor

**Choice**: Materialize a `Source.Queue → SelectAsyncUnordered(download) → SelectAsyncUnordered(mux) → Sink.ForEach(Self.Tell)` graph inside the DownloadQueueActor.

**Why over worker actors**: Worker actors add per-download overhead (actor creation, mailbox, supervision config) with no benefit — downloads are stateless I/O operations, not entities needing individual identity. Streams give per-stage parallelism control (20 downloads, 4 muxes) and automatic backpressure. The DownloadQueueActor remains the single owner of persistent state.

**Why `SelectAsyncUnordered` over `SelectAsync`**: Download completion order doesn't matter. Unordered allows the stream to push completed elements immediately without head-of-line blocking from slow downloads.

**Why not replace the actor entirely with a standalone stream**: The actor owns event-sourced state, handles SABnzbd API queries, and manages the lifecycle across restarts. Streams don't have persistence — the actor is the right owner.

### Decision 2: Typed outcomes instead of stream supervision for business errors

**Choice**: Download and mux lambdas catch all exceptions internally and return typed `DownloadOutcome.Success`/`DownloadOutcome.Failure` (and `MuxOutcome` equivalents). Stream supervision is a safety net for unexpected errors only.

**Why**: Njord's pattern — `FetchOutcome.Success`/`FetchOutcome.Failure` flowing through the stream. When supervision resumes, it silently drops the element. For weather data that's fine (next tick retries). For downloads, a dropped element means a job stuck in "Downloading" forever with no failure notification. Typed outcomes guarantee every element reaches the sink and the actor learns the result.

**Supervision decider**: Resume on `TaskCanceledException`, `OperationCanceledException`. Stop on everything else (programming error — let the BackoffSupervisor restart the actor and re-materialize the stream).

### Decision 3: Actor state machine with Become + IWithStash

**Choice**: DownloadQueueActor uses three states: `Recovering` → `Materializing` → `Ready`.

- **Recovering**: Replays persisted events. Stashes incoming `EnqueueDownload` messages.
- **Materializing**: Builds the stream graph, materializes it. Stashes.
- **Ready**: Stream is running. Accepts enqueue, cancel, status queries. Pushes to `Source.Queue` via `OfferAsync`.

**Why**: Njord's `SchedulerActor` uses the same pattern (`WaitingForPipeline → Connecting → Ready`). Without stashing, messages arriving during stream setup would be lost or cause errors.

### Decision 4: MuxingService extracted from MuxingActor

**Choice**: Extract FFmpeg process management, subtitle normalization, and temp file cleanup from `MuxingActor` into a stateless `MuxingService` class registered in DI. The stream's mux stage calls `MuxingService.MuxAsync()`.

**Why**: The mux logic has no actor-specific dependencies (no `Context`, no `Sender`). It's pure I/O: run FFmpeg, wait, return result. Extracting it makes it testable without actors and callable from the stream lambda.

### Decision 5: Matching pipeline as composed LINQ functions

**Choice**: `MatchingPipeline.Execute(items, context)` chains: `Where(NotJunk) → Where(MatchesShow) → Where(MatchesEpisode) → Where(DurationOk) → SelectMany(ExpandQualities) → Select(Score) → OrderByDescending(Score)`.

`MatchContext` is a record carrying: `ShowName`, `Season?`, `Episode?`, `AirDate?`, `ExpectedDurationSeconds?`, `ImdbId?`.

**Why not Akka.Streams**: The input is a batch (MediathekViewWeb returns up to 5000 items in one response). All stages are CPU-bound filters/transforms. No I/O, no parallelism needed. LINQ is faster (no materialization overhead), simpler, and directly testable.

**Why `MatchContext`**: Avoids parameter explosion. Each filter function takes `(item, context)` and can use whatever fields it needs. New matching criteria are added by extending the context record and adding a filter.

### Decision 6: Progress reporting via Self.Tell from stream lambda

**Choice**: The download lambda calls `Self.Tell(new ProgressUpdate(nzoId, downloaded, total))` during the download loop (throttled to every 2 seconds, same as current worker).

**Why**: `Self` (the actor ref) is thread-safe and can be captured in the lambda closure. No need for a separate progress channel. Progress updates are not persisted (same as today), so the actor just updates in-memory job state.

### Decision 7: SharedKillSwitch for cancellation

**Choice**: The actor holds a `SharedKillSwitch` instance. The stream graph includes `.Via(killSwitch.Flow<T>())`. To cancel the pipeline (e.g., during shutdown or re-materialization), call `killSwitch.Shutdown()`.

**Why**: Njord's `StreamConsumerActor` pattern. Cleaner than tracking individual `CancellationTokenSource` per download. Individual download cancellation uses `CancellationTokenSource` per element inside the lambda — the KillSwitch is for pipeline-level teardown.

## Risks / Trade-offs

**[Risk] 20 concurrent downloads overwhelm CDN or bandwidth** → Start with configurable defaults. Users can lower `ConcurrentDownloads` if their connection or CDN throttles. No built-in rate limiting initially — add a `BudgetThrottleStage` (Njord pattern) later if needed.

**[Risk] Source.Queue backpressure blocks the actor mailbox** → `OfferAsync` returns a `Task<QueueOfferResult>`. If the queue is full (64 elements), the offer blocks. Since we `PipeTo(Self)` the result, the actor remains responsive to other messages. If backpressure is sustained, new enqueues wait — which is correct behavior.

**[Risk] Stream failure during muxing loses downloaded file** → Typed outcomes ensure the actor always learns about failures. On mux failure, temp files are preserved (current behavior). On stream-level crash (supervision Stop), the actor restarts via BackoffSupervisor, replays events, and re-materializes. Jobs in `Downloading`/`Muxing` state are reset to `Queued` on recovery (current behavior).

**[Risk] DownloadQueueActor becomes too large** → The actor gains stream materialization logic but loses worker management logic. Net complexity is similar. The stream pipeline is declarative (a few lines of graph construction), not imperative control flow.

**[Trade-off] Removing DownloadWorkerActor loses per-download supervision** → Worker supervision (restart on failure) is replaced by typed outcomes and retry logic in the actor. If a download fails, the actor can re-push it to the queue. This is simpler than actor supervision strategies and gives the actor full control over retry policy.

**[Trade-off] MuxingActor removal means no independent mux scaling** → Mux parallelism is controlled by the `SelectAsyncUnordered(N)` parameter. If future requirements need independent mux scaling (e.g., mux requests from outside the download pipeline), a new MuxingActor can be reintroduced that wraps `MuxingService`.
