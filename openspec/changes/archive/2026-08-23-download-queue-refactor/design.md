## Context

The `DownloadQueueActor` is a 441-line `ReceivePersistentActor` that owns five responsibilities: state projection (7 `ApplyEvent` overloads + `_jobs` dictionary), event sourcing (Persist/Recover with DTO mapping), stream pipeline construction (`GraphDsl.Create` with Partition/Merge), stream lifecycle management (materialization, kill switch, re-materialization), and metrics (counters, histograms, gauges). It has six constructor dependencies, zero test coverage, and mixes transient progress reporting into the domain event flow.

The reference project njord uses linear `.Via().To().RunWith()` chaining for stream pipelines, and only falls back to `GraphDsl` when fan-out is truly needed. Stream-owning actors are separated from state-owning actors.

## Goals / Non-Goals

**Goals:**
- Make the download queue state independently unit-testable without any Akka infrastructure
- Separate stream pipeline lifecycle from persistent state management into distinct actors
- Simplify the stream pipeline to linear `.Via()` chaining where the consumer sees a clean `Flow` shape
- Remove transient progress reporting from the actor mailbox
- Reduce the constructor dependency count for both resulting actors

**Non-Goals:**
- Changing the persistence format or event schema (existing journals must replay unchanged)
- Introducing `StreamConsumerActor` base class from njord (that can come later)
- Changing the SABnzbd API contract or queue API response shape
- Adding retry logic or changing the supervision strategy
- Migrating to MergeHub/BroadcastHub (the partition is internal to a single flow)

## Decisions

### Decision: Extract state into DownloadQueueState as a mutable class

The `_jobs` dictionary and all `ApplyEvent` overloads move into a `DownloadQueueState` class. It stays mutable (mutates the internal dictionary) rather than returning new instances on each apply — the actor is single-threaded so immutability adds allocation cost without safety benefit. The class exposes query methods (`ActiveJobs`, `History`, `ResetInFlight`) that the actor delegates to.

**Alternative considered:** Immutable state with `Apply()` returning a new instance. Rejected because the `with` copy on every event during recovery of potentially thousands of events would create unnecessary GC pressure, and the single-threaded actor model already guarantees safe mutation.

### Decision: DownloadPipelineActor as child actor with IServiceProvider

The stream pipeline moves into a `DownloadPipelineActor` created as a child of the `DownloadQueueActor`. It receives `IServiceProvider` and `DownloadOptions` to resolve its own service dependencies. The queue actor creates it via `Context.ActorOf` in `OnRecoveryCompleted`.

The pipeline actor handles three messages:
- `OfferDownload(DownloadRequest)` — offers to `Source.Queue`
- `StreamCompleted` — re-materializes, tells parent
- `Status.Failure` — re-materializes, tells parent to reset in-flight jobs

Domain events from stream stages (`DownloadStarted`, `DownloadCompleted`, etc.) go directly to `Context.Parent` — the pipeline actor never sees them.

**Alternative considered:** Keeping the stream in the queue actor and only extracting state. Rejected because the 6 service dependencies would still be required on the queue actor, and testing persistence logic would still require mocking all stream-related services.

### Decision: Unified download flow via Flow.FromGraph with internal GraphDsl

`DownloadStages.Mp4Download` and `DownloadStages.HlsDownload` merge into a single `DownloadStages.Download()` that returns `Flow<DownloadRequest, VideoDownloadResult, NotUsed>`. Internally it uses `Flow.FromGraph(GraphDsl.Create(...))` with `Partition` and `Merge` to route by source type. Externally it's a plain flow used via `.Via()`.

The pipeline actor's materialization becomes a linear chain:
```
Source.Queue → KillSwitch → Download → Subtitle → Normalize → Mux → Sink.ActorRef
```

**Alternative considered:** PartitionHub + MergeHub with PreMaterialize. Rejected for this case because the fan-out is fixed (exactly 2 branches, always the same) and internal to one pipeline — dynamic hub topology is overkill. The `Flow.FromGraph` approach encapsulates the same routing without managing separate stream lifecycles.

**Alternative considered:** Single `SelectAsyncUnordered` that dispatches internally via `if`. Simpler but loses the ability to test HLS and MP4 flows independently and hides the routing topology.

### Decision: Thread OutputDir and Title through stream pipeline models

`VideoDownloadResult`, `SubtitleResult`, and `NormalizedResult` gain `OutputDir` and `Title` fields, populated from the `DownloadRequest` at the download stage. `MuxStages.Remux` reads these from the incoming `NormalizedResult` instead of calling a `LookupJob` callback.

This eliminates the cross-actor-boundary state lookup and makes the stream fully self-contained — every element carries all the data it needs.

### Decision: IProgress\<DownloadProgress\> on DownloadRequest

Each `DownloadRequest` carries an `IProgress<DownloadProgress>` instance. The `DownloadQueueActor` creates a `DownloadProgress` object per job and a `Progress<DownloadProgress>` callback that updates it. The `DownloadProgress` object is stored alongside the job (not on `DownloadJob` itself, which is the persistent projection).

`DownloadService` and `HlsDownloadService` change their signature from `Action<long, long> onProgress` to `IProgress<DownloadProgress> progress`. The stream stages call `req.Progress.Report(...)` instead of `self.Tell(ProgressUpdated)`.

Controllers read progress by accessing the shared `DownloadProgress` object — the actor provides it when responding to `GetQueue`.

`DownloadProgressUpdated` event, `HandleProgressUpdate`, and the progress fields on `DownloadJob` are all removed.

### Decision: DownloadQueueActor keeps IServiceProvider for child creation

The queue actor takes `IServiceProvider` and `IOptions<DownloadOptions>`. It uses the service provider to create the pipeline child actor via `DependencyResolver`. The actor no longer directly depends on any download/mux service.

## Risks / Trade-offs

**[Risk: Progress object thread safety]** → `DownloadProgress` is written from stream threads and read from API threads. Fields are simple `long`/`double` values — use `volatile` or `Interlocked` for atomic reads. Tearing on 64-bit fields is not possible on x64 but using `Volatile.Read`/`Volatile.Write` makes the intent explicit.

**[Risk: Pipeline actor lifecycle during recovery]** → The pipeline actor must only be created after recovery completes. The queue actor creates it in `OnRecoveryCompleted`, not in the constructor. If the pipeline actor crashes, the queue actor must re-create it — use standard child supervision.

**[Risk: Breaking existing journal entries]** → No risk. The persistence layer (DTOs, events, serialization) is unchanged. `DownloadProgressUpdated` was never persisted. No migration needed.

**[Trade-off: IServiceProvider vs explicit dependencies]** → Using `IServiceProvider` hides dependencies at compile time. Accepted because the pipeline actor's dependencies are an implementation detail of the stream construction, and explicit listing of 5+ services in a child actor constructor adds noise without improving testability (tests replace the entire service provider).
