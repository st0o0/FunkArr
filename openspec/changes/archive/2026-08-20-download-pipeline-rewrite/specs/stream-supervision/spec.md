## MODIFIED Requirements

### Requirement: SharedKillSwitch for pipeline lifecycle
The system SHALL use a `SharedKillSwitch` to coordinate stream teardown. The KillSwitch MUST be applied to the stream graph via `.Via(killSwitch.Flow<T>())`. A `CancellationTokenSource` SHALL be linked to the KillSwitch lifecycle: cancelled in `PostStop()` before KillSwitch shutdown, and its token passed to stage factories for cooperative cancellation of in-flight async operations.

#### Scenario: Pipeline shutdown on actor stop
- **WHEN** the DownloadQueueActor is stopping (PostStop lifecycle)
- **THEN** the actor cancels the CancellationTokenSource, then calls `killSwitch.Shutdown()`, and the stream terminates gracefully with in-flight async operations observing the cancellation

#### Scenario: Pipeline re-materialization after failure
- **WHEN** the stream terminates due to a supervision Stop directive
- **THEN** the actor shuts down the old KillSwitch, disposes the old CancellationTokenSource, creates new instances of both, re-materializes the stream, and re-pushes all jobs with status `Queued` into the new Source.Queue

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
