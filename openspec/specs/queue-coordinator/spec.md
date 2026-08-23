## Purpose

Centralized download scheduling actor that controls concurrency, ordering, and lifecycle of download jobs. Sits between API controllers and `DownloadQueueActor`, generating nzoIds, enforcing `MaxConcurrent`, and persisting scheduling events via event-sourcing.

## Requirements

### Requirement: QueueCoordinator singleton registration
The system SHALL register a `QueueCoordinator` actor as a resolvable Singleton in the ActorSystem.

#### Scenario: Registration
- **WHEN** the application starts
- **THEN** `QueueCoordinator` SHALL be registered and resolvable via `IActorRegistry`

### Requirement: Event-sourced scheduling persistence
`QueueCoordinator` SHALL be a `ReceivePersistentActor` with `PersistenceId: "queue-coordinator"`. It SHALL persist scheduling events: `JobEnqueued`, `JobStarted`, `JobFinished`, `JobRemoved`.

#### Scenario: Enqueue persisted
- **WHEN** a new download is enqueued
- **THEN** `QueueCoordinator` SHALL persist a `JobEnqueued` event before acknowledging

#### Scenario: Recovery reconstructs queue
- **WHEN** `QueueCoordinator` restarts after a crash
- **THEN** it SHALL replay all events to reconstruct `_queue` and `_active` sets, reset all `_active` entries back to `_queue`, and call `TryStartNext()`

### Requirement: Queue ordering and MaxConcurrent enforcement
`QueueCoordinator` SHALL maintain a `_queue` (ordered list of pending jobs), `_active` (set of currently running nzoIds), and `_maxConcurrent` (from config). `TryStartNext()` SHALL start queued jobs when `_active.Count < _maxConcurrent`.

#### Scenario: Job started when slot available
- **WHEN** a job is enqueued and `_active.Count < _maxConcurrent`
- **THEN** `QueueCoordinator` SHALL immediately start the job by telling `DownloadQueueActor` and persisting `JobStarted`

#### Scenario: Job queued when at capacity
- **WHEN** a job is enqueued and `_active.Count >= _maxConcurrent`
- **THEN** the job SHALL remain in `_queue` until a slot opens

#### Scenario: Next job started after slot freed
- **WHEN** `QueueCoordinator` receives `JobFinished` for an active job
- **THEN** it SHALL remove the job from `_active`, persist `JobFinished`, and call `TryStartNext()` to start the next queued job

### Requirement: Enqueue generates nzoId and replies
`QueueCoordinator` SHALL generate a unique `nzoId` for each enqueued download and reply with the nzoId to the sender.

#### Scenario: Enqueue reply
- **WHEN** a controller sends `Enqueue(downloadUrl, title, subtitleUrl)`
- **THEN** `QueueCoordinator` SHALL generate a 10-character hex nzoId, persist `JobEnqueued`, and reply with the nzoId

### Requirement: Cancel removes queued jobs
`QueueCoordinator` SHALL handle `Cancel(nzoId)` by removing the job from `_queue` if queued, persisting `JobRemoved`.

#### Scenario: Cancel queued job
- **WHEN** `Cancel("abc123")` is received and "abc123" is in `_queue`
- **THEN** `QueueCoordinator` SHALL remove it from `_queue` and persist `JobRemoved`

#### Scenario: Cancel active job (Phase 2a limitation)
- **WHEN** `Cancel("abc123")` is received and "abc123" is in `_active`
- **THEN** `QueueCoordinator` SHALL log a warning (active download cancellation requires Phase 2c)

### Requirement: GetQueueOrder returns ordered nzoIds
`QueueCoordinator` SHALL respond to `GetQueueOrder` with the ordered list of queued and active nzoIds.

#### Scenario: Queue order query
- **WHEN** `GetQueueOrder` is received with 2 active and 3 queued jobs
- **THEN** `QueueCoordinator` SHALL reply with active jobs first, then queued jobs in order

### Requirement: Metrics
`QueueCoordinator` SHALL emit `funkarr_queue_depth` gauge reflecting the total number of active + queued jobs.

#### Scenario: Depth updated on enqueue
- **WHEN** a job is enqueued
- **THEN** `funkarr_queue_depth` SHALL be updated to reflect the new total
