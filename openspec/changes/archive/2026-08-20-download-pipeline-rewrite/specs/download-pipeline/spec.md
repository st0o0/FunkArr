## ADDED Requirements

### Requirement: Sink.ActorRef as stream termination
The download pipeline SHALL use `Sink.ActorRef<MuxOutcome>(self, completionMessage)` as the stream sink instead of `Sink.ForEach`. Stream outcomes (`MuxOutcome.Success`, `MuxOutcome.Failure`, `MuxOutcome.Skipped`) SHALL arrive as regular actor messages through the mailbox. The completion message SHALL be a `StreamCompleted` record sent when the stream terminates normally.

#### Scenario: Mux success delivered via actor mailbox
- **WHEN** the remux stage produces a `MuxOutcome.Success`
- **THEN** the `MuxOutcome.Success` message arrives in the actor's mailbox via `Sink.ActorRef`, the actor persists a `MuxingCompleted` event, records metrics, and triggers temp-file cleanup

#### Scenario: Mux failure delivered via actor mailbox
- **WHEN** the remux stage produces a `MuxOutcome.Failure`
- **THEN** the `MuxOutcome.Failure` message arrives in the actor's mailbox, the actor persists a `MuxingFailed` event and records metrics

#### Scenario: Stream normal completion
- **WHEN** the stream completes normally (Source.Queue completed, all in-flight elements drained)
- **THEN** `Sink.ActorRef` sends the `StreamCompleted` message to the actor, which re-materializes the stream and re-pushes queued jobs

#### Scenario: Stream failure notification
- **WHEN** the stream terminates with an error
- **THEN** the materialized `Task` faults, `PipeTo` sends a `StreamFailed` message to the actor, which re-materializes and resets in-flight jobs

### Requirement: Temp-file cleanup after muxing
The download pipeline SHALL include a cleanup stage after the remux stage that deletes temporary files on successful mux. On `MuxOutcome.Failure` or `MuxOutcome.Skipped`, temp files SHALL be preserved for debugging.

#### Scenario: Cleanup after successful mux
- **WHEN** the remux stage produces a `MuxOutcome.Success` with a source video path and optional intermediate subtitle path
- **THEN** the cleanup stage deletes the source video and intermediate subtitle files from `TempPath`, and the `MuxOutcome.Success` passes through to the sink unchanged

#### Scenario: No cleanup after failed mux
- **WHEN** the remux stage produces a `MuxOutcome.Failure`
- **THEN** the cleanup stage passes the outcome through without deleting any temp files

### Requirement: CancellationToken propagation via linked token source
The download pipeline SHALL create a `CancellationTokenSource` linked to the stream lifecycle. The token SHALL be passed to all stage factories and propagated to `DownloadAsync`, `HlsDownloadService.DownloadAsync`, `SubtitleAcquisitionService.AcquireAsync`, and `MuxingService.MuxAsync`. The `CancellationTokenSource` SHALL be cancelled in `PostStop()` before KillSwitch shutdown.

#### Scenario: Actor stop cancels in-flight downloads
- **WHEN** the DownloadQueueActor is stopping and downloads are in progress
- **THEN** the CancellationTokenSource is cancelled, in-flight async operations observe the token and abort, and the KillSwitch shuts down the stream graph

#### Scenario: Default operation without cancellation
- **WHEN** the actor is running normally and downloads are in progress
- **THEN** the CancellationToken is not cancelled and async operations complete normally

### Requirement: Source type routing via Partition and Merge
The download pipeline SHALL use `GraphDSL` with a `Partition` stage to route `DownloadRequest` elements by source type (HLS vs. Direct/MP4) to separate download flows. Results from both branches SHALL be merged via a `Merge` stage before flowing into the common subtitle acquisition path.

#### Scenario: MP4 request routed to MP4 flow
- **WHEN** a `DownloadRequest` with a direct MP4 URL enters the pipeline
- **THEN** the Partition stage routes it to the MP4 download flow, which calls `DownloadService.DownloadAsync`

#### Scenario: HLS request routed to HLS flow
- **WHEN** a `DownloadRequest` with an HLS manifest URL enters the pipeline
- **THEN** the Partition stage routes it to the HLS download flow, which calls `HlsDownloadService.DownloadAsync`

#### Scenario: Mixed requests processed concurrently
- **WHEN** the queue contains both MP4 and HLS download requests
- **THEN** both flow branches process concurrently within the shared parallelism limit, and all results merge into the common subtitle/normalize/remux path

## MODIFIED Requirements

### Requirement: Concurrent download workers
The system SHALL execute downloads concurrently up to a configurable maximum (default 3) using a `GraphDSL`-based Akka Streams pipeline materialized inside the DownloadQueueActor. The graph SHALL use a `Partition` stage to route requests by URL type (.mp4 vs .m3u8) to separate download flows, then merge results into a common path for subtitle acquisition, normalization, and remuxing. The pipeline SHALL terminate with `Sink.ActorRef` delivering typed `MuxOutcome` messages directly to the actor mailbox.

#### Scenario: Concurrency limit respected
- **WHEN** 5 downloads are queued and the concurrency limit is 3
- **THEN** 3 downloads run simultaneously inside the stream pipeline (across both MP4 and HLS branches) and 2 jobs remain in "Queued" status waiting for backpressure to release

#### Scenario: Worker completes and next job starts
- **WHEN** a download completes inside the stream pipeline and there are queued jobs remaining
- **THEN** the DownloadQueueActor offers the next queued job to the Source.Queue and the stream picks it up for processing

#### Scenario: Backpressure from downstream stages
- **WHEN** the subtitle, normalization, or remux stages are saturated and more downloads complete
- **THEN** the stream applies backpressure and completed downloads wait in the buffer until a downstream slot opens

#### Scenario: Mixed MP4 and HLS downloads run concurrently
- **WHEN** the queue contains both MP4 and HLS download requests
- **THEN** both types process concurrently within the same pipeline, sharing the total concurrency limit

### Requirement: Pipeline stage composition
The download pipeline SHALL be composed of discrete, independently testable stream stages connected via `GraphDSL`. Each stage SHALL be a static method or factory returning a `Flow<TIn, TOut, NotUsed>`. All stage factories SHALL accept a `CancellationToken` parameter for cooperative cancellation.

#### Scenario: Stage isolation
- **WHEN** the subtitle normalization stage throws an exception
- **THEN** the stream supervision decider handles it independently of the download and remux stages

#### Scenario: Stages are independently testable
- **WHEN** a developer writes a test for the subtitle normalization stage
- **THEN** the stage can be materialized and tested with `Source.Single` and `Sink.First` without requiring the full pipeline graph

## REMOVED Requirements

### Requirement: Shared stage parallelism
**Reason**: Requirement text was correct but misleading — the implementation used a single lambda instead of composed stages. The parallelism model remains `ConcurrentDownloads` shared across all stages, but this is now an emergent property of passing the same value to each stage factory, not a separately stated requirement.
**Migration**: No migration needed. `ConcurrentDownloads` continues to be the sole parallelism setting.
