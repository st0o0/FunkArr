## ADDED Requirements

### Requirement: Child actor owning stream lifecycle
`DownloadPipelineActor` SHALL be a `ReceiveActor` in the `FunkArr.DownloadClient` namespace, created as a child of `DownloadQueueActor`. It SHALL own the Akka Streams download pipeline lifecycle: `IMaterializer`, `ISourceQueueWithComplete<DownloadRequest>`, `SharedKillSwitch`, and `CancellationTokenSource`.

#### Scenario: Created as child of DownloadQueueActor
- **WHEN** the `DownloadQueueActor` completes recovery
- **THEN** it creates a `DownloadPipelineActor` as a child via `Context.ActorOf`

### Requirement: Linear pipeline materialization
The pipeline actor SHALL materialize the download stream as a linear `.Via()` chain:
`Source.Queue<DownloadRequest>` → KillSwitch → `DownloadStages.Download()` → `SubtitleStages.Acquire()` → `SubtitleStages.Normalize()` → `MuxStages.Remux()` → `Sink.ActorRef<MuxOutcome>(Context.Parent)`.

The `Sink.ActorRef` SHALL target `Context.Parent` (the queue actor) so that `MuxOutcome` messages arrive in the persistent actor's mailbox.

#### Scenario: Pipeline materialized as linear chain
- **WHEN** the pipeline actor starts
- **THEN** it materializes a stream with `.Via()` chaining, no top-level `GraphDsl.Create` or `RunnableGraph.FromGraph`

#### Scenario: Stream outcomes delivered to parent
- **WHEN** the remux stage produces a `MuxOutcome.Success`
- **THEN** it arrives in the `DownloadQueueActor` mailbox via `Sink.ActorRef(Context.Parent)`

### Requirement: Domain events reported to parent
All stream stages that report domain events (`DownloadStarted`, `DownloadCompleted`, `DownloadFailed`, `MuxingStarted`) SHALL send them to `Context.Parent` (the queue actor), not to the pipeline actor itself. The `IActorRef` passed to stage factories SHALL be `Context.Parent`.

#### Scenario: DownloadStarted goes to queue actor
- **WHEN** a download stage begins processing a request
- **THEN** it sends `DownloadEvents.DownloadStarted` to `Context.Parent`

### Requirement: Offer download to stream
The pipeline actor SHALL handle an `OfferDownload(DownloadRequest)` message by calling `queue.OfferAsync(request)` and piping the result back to itself for logging/error handling.

#### Scenario: Successful offer
- **WHEN** the pipeline actor receives `OfferDownload` and the stream queue has capacity
- **THEN** the request is offered to the `Source.Queue` and enters the pipeline

#### Scenario: Offer when queue is full
- **WHEN** the `Source.Queue` buffer is full
- **THEN** `OfferAsync` applies backpressure and the offer completes when a slot opens

### Requirement: Stream re-materialization on failure
The pipeline actor SHALL handle `StreamCompleted` and `Status.Failure` messages by re-materializing the stream. On `Status.Failure`, the actor SHALL tell the parent to reset in-flight jobs before re-pushing queued jobs.

#### Scenario: Stream completes normally
- **WHEN** the stream completes (source queue completed)
- **THEN** the pipeline actor re-materializes and tells the parent to re-push queued jobs

#### Scenario: Stream fails with exception
- **WHEN** the stream terminates with an error
- **THEN** the pipeline actor re-materializes, tells the parent to reset in-flight jobs, and the parent re-pushes queued jobs

### Requirement: Cleanup on stop
The pipeline actor SHALL cancel its `CancellationTokenSource`, shut down its `SharedKillSwitch`, and complete its `Source.Queue` in `PostStop()`.

#### Scenario: Actor stop tears down stream
- **WHEN** the pipeline actor is stopped
- **THEN** `CancellationTokenSource` is cancelled, `KillSwitch` is shut down, and `Source.Queue` is completed

### Requirement: Service resolution via IServiceProvider
The pipeline actor SHALL receive `IServiceProvider` and `DownloadOptions` and resolve `DownloadService`, `HlsDownloadService`, `MuxingService`, `SubtitleAcquisitionService`, and `IFileService` from the service provider during construction.

#### Scenario: Services resolved at construction
- **WHEN** the pipeline actor is created with an `IServiceProvider`
- **THEN** all five service dependencies are resolved and available for stream construction
