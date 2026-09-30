## MODIFIED Requirements

### Requirement: DownloadQueueActor receives start commands from QueueCoordinator
`DownloadQueueActor` SHALL accept `EnqueueDownload` from `QueueCoordinator` (instead of directly from controllers). On download completion or failure, it SHALL notify `QueueCoordinator` with `JobFinished(nzoId, outcome)` so the scheduling slot is freed.

#### Scenario: Completion notifies QueueCoordinator
- **WHEN** a download completes successfully (MuxOutcome.Success)
- **THEN** `DownloadQueueActor` SHALL tell `QueueCoordinator` with `JobFinished(nzoId, "success")`

#### Scenario: Failure notifies QueueCoordinator
- **WHEN** a download fails (DownloadFailed or MuxOutcome.Failure)
- **THEN** `DownloadQueueActor` SHALL tell `QueueCoordinator` with `JobFinished(nzoId, "failed")`
