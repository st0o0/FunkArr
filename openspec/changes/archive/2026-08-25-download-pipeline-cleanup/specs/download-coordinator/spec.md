## ADDED Requirements

### Requirement: SubtitleDetected event persistence
DownloadActor SHALL persist a `SubtitleDetected(nzoId, found)` domain event when it receives `SubtitleAcquired` from a subtitle worker. Recovery SHALL replay this event to restore the `_hasSubtitle` flag.

#### Scenario: SubtitleDetected persisted on acquisition
- **WHEN** `SubtitleAcquired(nzoId, found: true)` is received from a subtitle worker
- **THEN** DownloadActor SHALL persist `SubtitleDetected(nzoId, found: true)` before proceeding to the next stage

#### Scenario: Recovery restores HasSubtitle
- **WHEN** DownloadActor recovers and replays a `SubtitleDetected(nzoId, found: true)` event
- **THEN** `_hasSubtitle` SHALL be `true`

#### Scenario: SubtitleDetected with found false
- **WHEN** `SubtitleAcquired(nzoId, found: false)` is received
- **THEN** DownloadActor SHALL persist `SubtitleDetected(nzoId, found: false)` and set `_hasSubtitle = false`

### Requirement: Terminated handler transitions to Failed
DownloadActor SHALL handle `Terminated` messages for child worker actors. If `Terminated` is received and no `WorkerFailed` was processed in the current stage, the actor SHALL treat it as an unexpected crash: persist `JobFailed(FailureKind.Transient, "Worker terminated unexpectedly")`, notify DownloadRequestActor and QueueActor, and transition to the Completed (terminal) state. A `_workerFailedReceived` flag SHALL be reset on each stage entry.

#### Scenario: Worker crashes without sending WorkerFailed
- **WHEN** a child `HlsDownloadActor` is stopped by the supervisor due to an unhandled exception and `Terminated` arrives without a prior `WorkerFailed`
- **THEN** DownloadActor SHALL persist `JobFailed(Transient, "Worker terminated unexpectedly")` and transition to terminal state

#### Scenario: Normal failure followed by Terminated
- **WHEN** `WorkerFailed` is received followed by `Terminated` for the same worker
- **THEN** DownloadActor SHALL ignore the `Terminated` because `_workerFailedReceived` is already set

### Requirement: Notify HistoryActor on completion
On job completion, DownloadActor SHALL tell HistoryActor with `RecordCompletion(nzoId, title, category, outputPath, downloadUrl, subtitleUrl, completedAt)`. On job failure, it SHALL tell HistoryActor with `RecordFailure(nzoId, title, category, error, downloadUrl, subtitleUrl, failedAt)`. HistoryActor SHALL be resolved from `IActorRegistry`.

#### Scenario: Completion notified to HistoryActor
- **WHEN** a download completes successfully
- **THEN** DownloadActor SHALL tell HistoryActor with `RecordCompletion` containing the full job metadata

#### Scenario: Failure notified to HistoryActor
- **WHEN** a download fails
- **THEN** DownloadActor SHALL tell HistoryActor with `RecordFailure` containing the error and original download parameters

## MODIFIED Requirements

### Requirement: Status updates to DownloadRequestActor
On each stage transition, the DownloadActor SHALL tell the DownloadRequestActor shard with `ReportProgress(nzoId, status, percentage, mb, mbleft)`. On stage entry (non-progress), percentage SHALL be 0, mb SHALL be 0.0, mbleft SHALL be 0.0. On completion, it SHALL tell `CompleteDownload`. On failure, it SHALL tell `FailDownload`.

#### Scenario: Status forwarded on stage change
- **WHEN** the coordinator enters the Muxing stage
- **THEN** it SHALL tell DownloadRequestActor with `ReportProgress(nzoId, "Muxing", 0, 0.0, 0.0)`

#### Scenario: Progress forwarded during download
- **WHEN** a `ProgressTick` is received from a worker
- **THEN** it SHALL tell DownloadRequestActor with `ReportProgress(nzoId, currentStatus, percentage, mb, mbleft)`
