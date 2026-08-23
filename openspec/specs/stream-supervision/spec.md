## Purpose

Stream-level fault tolerance for the Akka.Streams download pipeline: supervision decider, kill-switch lifecycle, typed outcome records, and actor state machine for stream materialization.

## Requirements

### Requirement: Stream supervision decider
The system SHALL provide a `StreamSupervision` utility that returns an Akka.Streams supervision decider. The decider MUST log every exception at Warning level before returning the directive. The decider MUST handle failure modes from all pipeline stages including HLS downloads and ffprobe calls.

#### Scenario: Transient cancellation errors resume the stream
- **WHEN** a `TaskCanceledException` or `OperationCanceledException` occurs in a stream stage
- **THEN** the supervision decider returns `Resume` and the element is dropped without stopping the stream

#### Scenario: Programming errors stop the stream
- **WHEN** an unexpected exception (e.g., `NullReferenceException`, `InvalidOperationException`) occurs in a stream stage
- **THEN** the supervision decider returns `Stop` and the stream terminates, triggering actor-level recovery

#### Scenario: FFmpeg process errors resume the stream
- **WHEN** an exception related to FFmpeg process management (e.g., `Win32Exception` from Process.Start, process killed timeout) occurs in the HLS download or subtitle extraction stage
- **THEN** the supervision decider returns `Resume` because the typed outcome already captured the failure, and other pipeline elements should continue

#### Scenario: HTTP errors in subtitle stage resume the stream
- **WHEN** an `HttpRequestException` occurs in the subtitle acquisition stage
- **THEN** the supervision decider returns `Resume` because missing subtitles are non-fatal

### Requirement: SharedKillSwitch for pipeline lifecycle
The system SHALL use a `SharedKillSwitch` to coordinate stream teardown. The KillSwitch MUST be applied to the stream graph via `.Via(killSwitch.Flow<T>())`. A `CancellationTokenSource` SHALL be linked to the KillSwitch lifecycle: cancelled in `PostStop()` before KillSwitch shutdown, and its token passed to stage factories for cooperative cancellation of in-flight async operations.

#### Scenario: Pipeline shutdown on actor stop
- **WHEN** the DownloadQueueActor is stopping (PostStop lifecycle)
- **THEN** the actor cancels the CancellationTokenSource, then calls `killSwitch.Shutdown()`, and the stream terminates gracefully with in-flight async operations observing the cancellation

#### Scenario: Pipeline re-materialization after failure
- **WHEN** the stream terminates due to a supervision Stop directive
- **THEN** the actor shuts down the old KillSwitch, disposes the old CancellationTokenSource, creates new instances of both, re-materializes the stream, and re-pushes all jobs with status `Queued` into the new Source.Queue

### Requirement: Typed download outcomes
The system SHALL use typed outcome records (`DownloadOutcome.Success`, `DownloadOutcome.Failure`) flowing through the stream pipeline. Business errors MUST be caught inside stream lambdas and emitted as `Failure` outcomes — never relying on stream supervision for business error handling.

#### Scenario: Download HTTP error produces typed failure
- **WHEN** an `HttpRequestException` occurs during a download in the stream lambda
- **THEN** the lambda catches it and returns a `DownloadOutcome.Failure` with the NzoId and error message, the stream continues processing other elements

#### Scenario: Download success produces typed success
- **WHEN** a download and optional subtitle download complete successfully
- **THEN** the lambda returns a `DownloadOutcome.Success` with the NzoId, video path, and optional subtitle path

#### Scenario: Mux failure produces typed failure
- **WHEN** FFmpeg exits with a non-zero exit code during the mux stage
- **THEN** the mux lambda returns a `MuxOutcome.Failure` with the NzoId and error message, the stream continues processing other elements

#### Scenario: Mux skipped produces typed outcome
- **WHEN** muxing is not needed for a download
- **THEN** the mux lambda returns a `MuxOutcome.Skipped` with the NzoId, the stream continues processing other elements

#### Scenario: Every outcome reaches the actor
- **WHEN** any element enters the stream pipeline (whether it succeeds or fails)
- **THEN** a corresponding `Self.Tell` message reaches the DownloadQueueActor, ensuring no job is left in a stale state

### Requirement: Actor state machine for stream lifecycle
The DownloadQueueActor SHALL implement `IWithStash` and use `Become` to manage two states: `Recovering` and `Ready`. Stream materialization is a synchronous transition method that immediately materializes the stream and calls `Become(Ready)` — it is NOT a separate stashable state. Messages are not stashed during materialization because the transition is instant. After transitioning to `Ready`, all recovered jobs with `Queued` status SHALL be offered to the Source.Queue.

#### Scenario: Messages stashed during recovery
- **WHEN** an `EnqueueDownload` message arrives while the actor is in `Recovering` state
- **THEN** the message is stashed and delivered after the actor transitions to `Ready`

#### Scenario: Actor transitions to Ready after stream materialization
- **WHEN** recovery completes
- **THEN** the actor synchronously materializes the stream, calls `Become(Ready)`, unstashes all pending messages, and re-pushes all recovered `Queued` jobs into the Source.Queue

#### Scenario: Recovered queued jobs are re-offered to stream
- **WHEN** the application restarts with 3 queued jobs recovered from the journal
- **THEN** after stream materialization and `Become(Ready)`, the actor calls `PushQueuedJobs()` to offer all 3 jobs to the Source.Queue for processing
