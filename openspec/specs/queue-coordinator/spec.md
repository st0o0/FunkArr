## Purpose

Centralized download scheduling actor that controls concurrency, ordering, and lifecycle of download jobs. Sits between API controllers and `DownloadQueueActor`, generating nzoIds, enforcing `MaxConcurrent`, and persisting scheduling events via event-sourcing.

## Requirements

### Requirement: QueueActor singleton registration
The system SHALL register a `QueueActor` actor as a resolvable Singleton in the ActorSystem.

#### Scenario: Registration
- **WHEN** the application starts
- **THEN** `QueueActor` SHALL be registered and resolvable via `IActorRegistry`

### Requirement: Event-sourced scheduling persistence
`QueueActor` SHALL be a `ReceivePersistentActor` with `PersistenceId: "queue-coordinator"`. It SHALL persist scheduling events: `JobEnqueued`, `JobStarted`, `JobFinished`, `JobRemoved`.

#### Scenario: Enqueue persisted
- **WHEN** a new download is enqueued
- **THEN** `QueueActor` SHALL persist a `JobEnqueued` event before acknowledging

#### Scenario: Recovery reconstructs queue
- **WHEN** `QueueActor` restarts after a crash
- **THEN** it SHALL replay all events to reconstruct `_queue` and `_active` sets, reset all `_active` entries back to `_queue`, and call `TryStartNext()`

### Requirement: Queue ordering and MaxConcurrent enforcement
`QueueActor` SHALL maintain a `_queue` (ordered list of pending jobs), `_active` (set of currently running nzoIds), and `_maxConcurrent` (from config). `TryStartNext()` SHALL start queued jobs when `_active.Count < _maxConcurrent`. `TryStartNext()` SHALL build `StartDownload` messages without `_tempPath` or `_downloadPath` — those are `IFileService` concerns, not scheduling concerns.

#### Scenario: Job started when slot available
- **WHEN** a job is enqueued and `_active.Count < _maxConcurrent`
- **THEN** `QueueActor` SHALL immediately start the job by telling the DownloadActor shard with `StartDownload(nzoId, downloadUrl, subtitleUrl, title)` and persisting `JobStarted`

#### Scenario: QueueActor no longer stores path fields
- **WHEN** `QueueActor` is constructed with `IOptions<DownloadOptions>`
- **THEN** it SHALL read only `ConcurrentDownloads` from the options. It SHALL NOT store `_tempPath` or `_downloadPath` as instance fields.

#### Scenario: Job queued when at capacity
- **WHEN** a job is enqueued and `_active.Count >= _maxConcurrent`
- **THEN** the job SHALL remain in `_queue` until a slot opens

#### Scenario: Next job started after slot freed
- **WHEN** `QueueActor` receives `JobFinished` for an active job
- **THEN** it SHALL remove the job from `_active`, persist `JobFinished`, and call `TryStartNext()` to start the next queued job

### Requirement: Enqueue generates nzoId and replies
`QueueActor` SHALL generate a unique `nzoId` for each enqueued download and reply with the nzoId to the sender. The `Enqueue` message SHALL include an optional `Category` field.

#### Scenario: Enqueue reply with category
- **WHEN** a controller sends `Enqueue(downloadUrl, title, subtitleUrl, category: "tv")`
- **THEN** `QueueActor` SHALL generate a 10-character hex nzoId, persist `JobEnqueued` (including category), and reply with the nzoId

#### Scenario: Enqueue without category
- **WHEN** a controller sends `Enqueue(downloadUrl, title, subtitleUrl, category: null)`
- **THEN** `QueueActor` SHALL persist `JobEnqueued` with null category and reply with the nzoId

### Requirement: Category passed through to DownloadActor
When starting a download, `QueueActor` SHALL pass the stored category to `DownloadActor` via the `StartDownload` message. Category resolution to a filesystem path is NOT done here — it is deferred to `FileService` at mux time.

#### Scenario: Category forwarded on start
- **WHEN** a queued job with category `"tv"` is started
- **THEN** `QueueActor` SHALL pass `category: "tv"` in the `StartDownload` message

### Requirement: Category stored in queue state
`QueueActor` SHALL persist the category in the `JobEnqueued` event and reconstruct it during recovery so it is available when the job is eventually started.

#### Scenario: Category survives recovery
- **WHEN** `QueueActor` restarts and replays a `JobEnqueued` event with category `"movies"`
- **THEN** the recovered queue entry SHALL retain category `"movies"`

### Requirement: Cancel removes queued jobs
`QueueActor` SHALL handle `Cancel(nzoId)` by removing the job from `_queue` if queued, persisting `JobRemoved`.

#### Scenario: Cancel queued job
- **WHEN** `Cancel("abc123")` is received and "abc123" is in `_queue`
- **THEN** `QueueActor` SHALL remove it from `_queue` and persist `JobRemoved`

#### Scenario: Cancel active job (Phase 2a limitation)
- **WHEN** `Cancel("abc123")` is received and "abc123" is in `_active`
- **THEN** `QueueActor` SHALL log a warning (active download cancellation requires Phase 2c)

### Requirement: GetQueueOrder returns ordered nzoIds
`QueueActor` SHALL respond to `GetQueueOrder` with the ordered list of queued and active nzoIds, including category for each.

#### Scenario: Queue order query
- **WHEN** `GetQueueOrder` is received with 2 active and 3 queued jobs
- **THEN** `QueueActor` SHALL reply with active jobs first, then queued jobs in order

#### Scenario: Queue order includes category
- **WHEN** `GetQueueOrder` is received with jobs that have categories
- **THEN** the response SHALL include the category for each nzoId

### Requirement: Metrics
`QueueActor` SHALL emit `funkarr_queue_depth` gauge reflecting the total number of active + queued jobs.

#### Scenario: Depth updated on enqueue
- **WHEN** a job is enqueued
- **THEN** `funkarr_queue_depth` SHALL be updated to reflect the new total
