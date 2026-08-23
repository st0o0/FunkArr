## Purpose

Akka.Streams-based download pipeline with concurrent HTTP downloads, backpressure-aware muxing, and persistent state via Akka.Persistence + SQLite.

## Requirements

### Requirement: Concurrent download workers
The system SHALL execute downloads concurrently up to a configurable maximum (default 3) using an Akka Streams pipeline materialized inside the `DownloadPipelineActor` (child of `DownloadQueueActor`). The pipeline SHALL be composed as a linear `.Via()` chain with a unified download flow that routes requests by URL type internally. The pipeline SHALL terminate with `Sink.ActorRef` delivering typed `MuxOutcome` messages to `Context.Parent` (the queue actor).

#### Scenario: Concurrency limit respected
- **WHEN** 5 downloads are queued and the concurrency limit is 3
- **THEN** 3 downloads run simultaneously inside the stream pipeline (across both MP4 and HLS branches) and 2 jobs remain in "Queued" status waiting for backpressure to release

#### Scenario: Worker completes and next job starts
- **WHEN** a download completes inside the stream pipeline and there are queued jobs remaining
- **THEN** the `DownloadQueueActor` sends an `OfferDownload` message to the pipeline child actor, which offers the next queued job to the Source.Queue

#### Scenario: Backpressure from downstream stages
- **WHEN** the subtitle, normalization, or remux stages are saturated and more downloads complete
- **THEN** the stream applies backpressure and completed downloads wait in the buffer until a downstream slot opens

#### Scenario: Mixed MP4 and HLS downloads run concurrently
- **WHEN** the queue contains both MP4 and HLS download requests
- **THEN** both types process concurrently within the same pipeline, sharing the total concurrency limit

### Requirement: DownloadQueueActor receives start commands from QueueCoordinator
`DownloadQueueActor` SHALL accept `EnqueueDownload` from `QueueCoordinator` (instead of directly from controllers). On download completion or failure, it SHALL notify `QueueCoordinator` with `JobFinished(nzoId, outcome)` so the scheduling slot is freed.

#### Scenario: Completion notifies QueueCoordinator
- **WHEN** a download completes successfully (MuxOutcome.Success)
- **THEN** `DownloadQueueActor` SHALL tell `QueueCoordinator` with `JobFinished(nzoId, "success")`

#### Scenario: Failure notifies QueueCoordinator
- **WHEN** a download fails (DownloadFailed or MuxOutcome.Failure)
- **THEN** `DownloadQueueActor` SHALL tell `QueueCoordinator` with `JobFinished(nzoId, "failed")`

### Requirement: HTTP stream download
Each download stage element SHALL download content via the appropriate service based on URL type. Progress SHALL be reported via `IProgress<DownloadProgress>` carried on the `DownloadRequest`, not via actor messages.

#### Scenario: Successful download with progress
- **WHEN** a download processes a 500MB video file
- **THEN** the service reports progress via `req.Progress.Report(...)` every 2 seconds, and the file is written to the configured temp directory

#### Scenario: Download fails with transient HTTP error
- **WHEN** a download fails with an `HttpRequestException`
- **THEN** the stage catches the error and returns a `DownloadOutcome.Failure`, the queue actor persists a `DownloadFailed` event, and the stream continues processing other downloads

### Requirement: Pipeline stage composition
The download pipeline SHALL be composed of discrete, independently testable stream stages connected via `.Via()`. Each stage SHALL be a static method or factory returning a `Flow<TIn, TOut, NotUsed>`. All stage factories SHALL accept a `CancellationToken` parameter for cooperative cancellation. The unified download flow SHALL use `Flow.FromGraph(GraphDsl.Create(...))` internally to encapsulate HLS/MP4 routing but SHALL expose a clean `Flow<DownloadRequest, VideoDownloadResult, NotUsed>` shape externally.

#### Scenario: Stage isolation
- **WHEN** the subtitle normalization stage throws an exception
- **THEN** the stream supervision decider handles it independently of the download and remux stages

#### Scenario: Stages are independently testable
- **WHEN** a developer writes a test for the subtitle normalization stage
- **THEN** the stage can be materialized and tested with `Source.Single` and `Sink.First` without requiring the full pipeline graph

#### Scenario: Unified download flow testable as a single flow
- **WHEN** a developer writes a test for the download flow
- **THEN** the flow can be materialized as `Source.Single(request).Via(DownloadStages.Download(...)).RunWith(Sink.First, mat)` without knowledge of the internal Partition/Merge topology

### Requirement: Supervision and fault isolation
The DownloadQueueActor SHALL be supervised by a `BackoffSupervisor`. A failing download MUST NOT affect other downloads in the stream pipeline.

#### Scenario: One download fails, others continue
- **WHEN** a download for job-2 fails with a 403 Forbidden error
- **THEN** the lambda returns `DownloadOutcome.Failure` for job-2, all other downloads continue unaffected, and the actor marks job-2 as "Failed"

#### Scenario: Stream-level crash triggers actor restart
- **WHEN** the stream terminates due to an unexpected error (supervision Stop)
- **THEN** the BackoffSupervisor restarts the DownloadQueueActor, events are replayed, and a new stream is materialized. Jobs in `Downloading` or `Muxing` state are reset to `Queued`

### Requirement: Queue persistence
The DownloadQueueActor SHALL persist its state using Akka.Persistence with SQLite backend. Queue state MUST survive application restarts.

#### Scenario: Queue recovery after restart
- **WHEN** the application restarts with 3 queued and 1 in-progress download
- **THEN** the DownloadQueueActor recovers its state from the journal, resets the in-progress download to "Queued", materializes the stream, and re-pushes all queued jobs into the Source.Queue

#### Scenario: History survives restart
- **WHEN** the application restarts and there are 5 completed downloads in history
- **THEN** the history endpoint returns all 5 completed entries with their original metadata

### Requirement: Download output organization
The system SHALL write completed downloads to the configured output directory using the title from the download request as the filename.

#### Scenario: Completed download file placement
- **WHEN** a download for "Show.S01E03.GERMAN.1080p.WEB.h264-FA" completes and muxing finishes
- **THEN** the final MKV file is placed at `<output_dir>/Show.S01E03.GERMAN.1080p.WEB.h264-FA/Show.S01E03.GERMAN.1080p.WEB.h264-FA.mkv`

### Requirement: Source.Queue as actor-to-stream bridge
The `DownloadPipelineActor` SHALL use `Source.Queue<DownloadRequest>` to bridge between its mailbox and the stream pipeline. The `DownloadQueueActor` SHALL send `OfferDownload` messages to the pipeline child actor, which offers them to the queue via `OfferAsync`.

#### Scenario: Job offered to running stream
- **WHEN** the queue actor sends `OfferDownload` to the pipeline child
- **THEN** the pipeline actor calls `queue.OfferAsync(request)` and the download enters the stream pipeline

#### Scenario: Queue backpressure when pipeline is saturated
- **WHEN** the Source.Queue buffer (64 elements) is full because all download slots and downstream stages are occupied
- **THEN** `OfferAsync` returns a pending Task, both actors remain responsive to status queries, and the offer completes when a slot opens

### Requirement: Sink.ActorRef as stream termination
The download pipeline SHALL use `Sink.ActorRef<MuxOutcome>(Context.Parent, completionMessage)` as the stream sink. Stream outcomes SHALL arrive as regular actor messages in the `DownloadQueueActor` mailbox. The completion message (`StreamCompleted`) SHALL be sent to the pipeline actor itself for re-materialization handling.

#### Scenario: Mux success delivered to queue actor
- **WHEN** the remux stage produces a `MuxOutcome.Success`
- **THEN** the message arrives in the `DownloadQueueActor` mailbox via `Sink.ActorRef(Context.Parent)`, the actor persists a `MuxingCompleted` event, records metrics, and triggers temp-file cleanup

#### Scenario: Stream normal completion
- **WHEN** the stream completes normally
- **THEN** `Sink.ActorRef` sends the `StreamCompleted` message to the pipeline actor, which re-materializes and tells the parent to re-push queued jobs

### Requirement: Data flows forward through pipeline models
`VideoDownloadResult`, `SubtitleResult`, and `NormalizedResult` SHALL carry `OutputDir` and `Title` fields threaded from the `DownloadRequest`. `MuxStages.Remux` SHALL read `OutputDir` and `Title` from the incoming `NormalizedResult` instead of using a `LookupJob` callback.

#### Scenario: Output directory and title available at mux stage
- **WHEN** a `DownloadRequest` with OutputDir="/downloads" and Title="Show.S01E01" enters the pipeline
- **THEN** the `NormalizedResult` arriving at `MuxStages.Remux` carries OutputDir="/downloads" and Title="Show.S01E01"

#### Scenario: No cross-actor lookup needed
- **WHEN** the mux stage needs the output directory and title
- **THEN** it reads them from the stream element, not from a callback or actor query

### Requirement: Temp-file cleanup after muxing
The download pipeline SHALL include a cleanup stage after the remux stage that deletes temporary files on successful mux. On `MuxOutcome.Failure` or `MuxOutcome.Skipped`, temp files SHALL be preserved for debugging.

#### Scenario: Cleanup after successful mux
- **WHEN** the remux stage produces a `MuxOutcome.Success` with a source video path and optional intermediate subtitle path
- **THEN** the cleanup stage deletes the source video and intermediate subtitle files from `TempPath`, and the `MuxOutcome.Success` passes through to the sink unchanged

#### Scenario: No cleanup after failed mux
- **WHEN** the remux stage produces a `MuxOutcome.Failure`
- **THEN** the cleanup stage passes the outcome through without deleting any temp files

### Requirement: CancellationToken propagation via linked token source
The `DownloadPipelineActor` SHALL create a `CancellationTokenSource` linked to the stream lifecycle. The token SHALL be passed to all stage factories. The `CancellationTokenSource` SHALL be cancelled in `PostStop()` before KillSwitch shutdown.

#### Scenario: Pipeline actor stop cancels in-flight downloads
- **WHEN** the pipeline actor is stopping and downloads are in progress
- **THEN** the CancellationTokenSource is cancelled, in-flight async operations observe the token and abort, and the KillSwitch shuts down the stream graph

### Requirement: Source type routing via unified download flow
The download pipeline SHALL use a unified `DownloadStages.Download()` flow that internally routes `DownloadRequest` elements by source type (HLS vs. Direct/MP4) via `Flow.FromGraph(GraphDsl.Create(...))` with `Partition` and `Merge`. This replaces the top-level `GraphDsl.Create` with `RunnableGraph.FromGraph`. The consumer sees a plain `Flow<DownloadRequest, VideoDownloadResult, NotUsed>` and chains it with `.Via()`.

#### Scenario: MP4 request routed internally to MP4 flow
- **WHEN** a `DownloadRequest` with a direct MP4 URL enters the unified download flow
- **THEN** the internal Partition stage routes it to the MP4 download branch, which calls `DownloadService.DownloadAsync`

#### Scenario: HLS request routed internally to HLS flow
- **WHEN** a `DownloadRequest` with an HLS manifest URL enters the unified download flow
- **THEN** the internal Partition stage routes it to the HLS download branch, which calls `HlsDownloadService.DownloadAsync`
