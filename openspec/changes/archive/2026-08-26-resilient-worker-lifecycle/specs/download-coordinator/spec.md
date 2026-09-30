## ADDED Requirements

### Requirement: Per-stage ReceiveTimeout safety net
DownloadActor SHALL set a `ReceiveTimeout` on each stage entry to guarantee that no stage hangs indefinitely. The timeout SHALL reset on every received message (built-in Akka behavior). When `ReceiveTimeout` fires, the actor SHALL kill the current worker (if alive) and transition to the Failed terminal state via the existing `HandleWorkerFailed` path.

#### Scenario: Fetching stage timeout
- **WHEN** DownloadActor enters the Fetching stage and no message is received for 30 minutes
- **THEN** it SHALL send `PoisonPill` to the current worker and call `HandleWorkerFailed` with `FailureKind.Transient` and reason `"Stage timed out"`

#### Scenario: AcquiringSubtitle stage timeout
- **WHEN** DownloadActor enters the AcquiringSubtitle stage and no message is received for 5 minutes
- **THEN** it SHALL send `PoisonPill` to the current worker and transition to Failed

#### Scenario: ConvertingSubtitle stage timeout
- **WHEN** DownloadActor enters the ConvertingSubtitle stage and no message is received for 5 minutes
- **THEN** it SHALL send `PoisonPill` to the current worker and transition to Failed

#### Scenario: Muxing stage timeout
- **WHEN** DownloadActor enters the Muxing stage and no message is received for 15 minutes
- **THEN** it SHALL send `PoisonPill` to the current worker and transition to Failed

#### Scenario: Timeout resets on ProgressTick
- **WHEN** a `ProgressTick` is received during the Fetching stage
- **THEN** the ReceiveTimeout timer SHALL reset to 30 minutes (automatic Akka behavior)

#### Scenario: Timeout cleared in terminal state
- **WHEN** DownloadActor enters the Completed behavior
- **THEN** it SHALL clear the ReceiveTimeout via `Context.SetReceiveTimeout(null)`

### Requirement: OnPersistFailure resilience
DownloadActor SHALL override `OnPersistFailure` to force-notify downstream actors before the actor stops. This ensures QueueActor releases the slot and DownloadRequestActor updates status even when the journal write fails.

#### Scenario: Persistence failure releases queue slot
- **WHEN** a `Persist` call fails for any event (JobFailed, JobCompleted, StageEntered, etc.)
- **THEN** DownloadActor SHALL tell QueueActor with `NotifyJobFinished(nzoId, "failed")` and tell DownloadRequestActor with `FailDownload(nzoId, reason)` before the actor stops

#### Scenario: Persistence rejection releases queue slot
- **WHEN** a `Persist` call is rejected by the journal
- **THEN** DownloadActor SHALL tell QueueActor with `NotifyJobFinished(nzoId, "failed")` and tell DownloadRequestActor with `FailDownload(nzoId, reason)` before the actor stops

## MODIFIED Requirements

### Requirement: Terminated handler transitions to Failed
DownloadActor SHALL handle `Terminated` messages for child worker actors. If `Terminated` is received and no `WorkerFailed` was processed in the current stage, the actor SHALL treat it as an unexpected crash: persist `JobFailed(FailureKind.Transient, "Worker terminated unexpectedly")`, notify DownloadRequestActor and QueueActor, and transition to the Completed (terminal) state. The `_workerFailedReceived` flag SHALL be set inside the `Persist` callback of `HandleWorkerFailed`, NOT before the `Persist` call. The flag SHALL be reset to `false` on each stage entry.

#### Scenario: Worker crashes without sending WorkerFailed
- **WHEN** a child `HlsDownloadActor` is stopped by the supervisor due to an unhandled exception and `Terminated` arrives without a prior `WorkerFailed`
- **THEN** DownloadActor SHALL persist `JobFailed(Transient, "Worker terminated unexpectedly")` and transition to terminal state

#### Scenario: Normal failure followed by Terminated
- **WHEN** `WorkerFailed` is received and persisted successfully, followed by `Terminated` for the same worker
- **THEN** DownloadActor SHALL ignore the `Terminated` because `_workerFailedReceived` is set inside the Persist callback

#### Scenario: Persistence failure during WorkerFailed leaves Terminated active
- **WHEN** `WorkerFailed` is received but `Persist` fails before the callback executes
- **THEN** `_workerFailedReceived` SHALL still be `false` and the subsequent `Terminated` message SHALL trigger `HandleWorkerTerminated` as a fallback recovery path
