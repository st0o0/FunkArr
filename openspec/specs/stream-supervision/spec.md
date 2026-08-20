## Purpose

Stream-level fault tolerance for the Akka.Streams download pipeline: supervision decider, kill-switch lifecycle, typed outcome records, and actor state machine for stream materialization.

## Requirements

### Requirement: Stream supervision decider
The system SHALL provide a `StreamSupervision` utility that returns an Akka.Streams supervision decider. The decider MUST log every exception at Warning level before returning the directive.

#### Scenario: Transient cancellation errors resume the stream
- **WHEN** a `TaskCanceledException` or `OperationCanceledException` occurs in a stream stage
- **THEN** the supervision decider returns `Resume` and the element is dropped without stopping the stream

#### Scenario: Programming errors stop the stream
- **WHEN** an unexpected exception (e.g., `NullReferenceException`, `InvalidOperationException`) occurs in a stream stage
- **THEN** the supervision decider returns `Stop` and the stream terminates, triggering actor-level recovery

### Requirement: SharedKillSwitch for pipeline lifecycle
The system SHALL use a `SharedKillSwitch` to coordinate stream teardown. The KillSwitch MUST be applied to the stream graph via `.Via(killSwitch.Flow<T>())`.

#### Scenario: Pipeline shutdown on actor stop
- **WHEN** the DownloadQueueActor is stopping (PostStop lifecycle)
- **THEN** the actor calls `killSwitch.Shutdown()` and the stream terminates gracefully

#### Scenario: Pipeline re-materialization after failure
- **WHEN** the stream terminates due to a supervision Stop directive
- **THEN** the actor shuts down the old KillSwitch, creates a new KillSwitch, re-materializes the stream, and re-pushes all jobs with status `Queued` into the new Source.Queue

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

#### Scenario: Every outcome reaches the actor
- **WHEN** any element enters the stream pipeline (whether it succeeds or fails)
- **THEN** a corresponding `Self.Tell` message reaches the DownloadQueueActor, ensuring no job is left in a stale state

### Requirement: Actor state machine for stream lifecycle
The DownloadQueueActor SHALL implement `IWithStash` and use `Become` to manage three states: `Recovering`, `Materializing`, and `Ready`.

#### Scenario: Messages stashed during recovery
- **WHEN** an `EnqueueDownload` message arrives while the actor is in `Recovering` state
- **THEN** the message is stashed and delivered after the actor transitions to `Ready`

#### Scenario: Messages stashed during materialization
- **WHEN** an `EnqueueDownload` message arrives while the actor is in `Materializing` state
- **THEN** the message is stashed and delivered after the actor transitions to `Ready`

#### Scenario: Actor transitions to Ready after stream materialization
- **WHEN** the stream graph is successfully materialized
- **THEN** the actor transitions to `Ready` state, unstashes all pending messages, and begins processing enqueue requests by offering to the Source.Queue
