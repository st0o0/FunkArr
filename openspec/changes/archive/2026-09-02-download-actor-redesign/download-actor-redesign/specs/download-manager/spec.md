## MODIFIED Requirements

### Requirement: DownloadManager accepts AddDownload
The DownloadManager SHALL handle `AddDownload` messages by assigning a new `Guid` as `DownloadId`, persisting a `DownloadEnqueued` event, forwarding an `InitDownload` message to the DownloadWorker shard region, and responding with `DownloadAdded`.

#### Scenario: Successful add
- **WHEN** an `AddDownload` message is received
- **THEN** the Manager SHALL generate a new DownloadId
- **AND** persist a `DownloadEnqueued` event with DownloadId
- **AND** send `InitDownload` (with all metadata: DownloadId, Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category, OutputPath) to the Worker shard region
- **AND** respond with `DownloadAdded(DownloadId)`
- **AND** call DispatchNext to check if the download can start immediately

### Requirement: DownloadManager enforces concurrency limit
The DownloadManager SHALL limit the number of concurrent downloads to a configurable maximum (default 3). When a slot is available, the Manager SHALL persist a `DownloadDispatched` event and send a bare `StartDownload(DownloadId)` go-signal to the Worker shard region.

#### Scenario: Under capacity
- **WHEN** DispatchNext runs and fewer than the configured maximum downloads are in the Dispatched set
- **THEN** the Manager SHALL move the next Queued item to the Dispatched set
- **AND** persist a `DownloadDispatched` event with DownloadId
- **AND** send `StartDownload(DownloadId)` to the Worker shard region

#### Scenario: At capacity
- **WHEN** DispatchNext runs and the Dispatched set has reached the configured maximum
- **THEN** the Manager SHALL not dispatch any further downloads

#### Scenario: Slot freed
- **WHEN** a `SlotFree` message is received
- **THEN** the Manager SHALL persist a `DownloadDequeued` event for the DownloadId
- **AND** call DispatchNext

### Requirement: DownloadManager answers queue queries
The DownloadManager SHALL handle `QueryQueue` messages by fanning out `QueryWorkerStatus` to all Workers in its Queued and Dispatched sets, collecting responses, and building a `QueueResult`.

#### Scenario: Queue query with fan-out
- **WHEN** a `QueryQueue` message is received
- **THEN** the Manager SHALL send `QueryWorkerStatus` to each Worker in the Queued and Dispatched sets via the shard region
- **AND** collect responses with a timeout of 2 seconds
- **AND** respond with a `QueueResult` built from Worker responses
- **AND** Workers that do not respond within the timeout SHALL be represented with zero progress

### Requirement: DownloadManager handles delete
The DownloadManager SHALL handle `DeleteDownload` messages for items in its queue (Queued or Dispatched) by dequeuing and cancelling the Worker.

#### Scenario: Delete queued or dispatched download
- **WHEN** a `DeleteDownload` is received for a DownloadId in the Queued or Dispatched set
- **THEN** the Manager SHALL persist a `DownloadDequeued` event
- **AND** send `CancelDownload` to the Worker shard region
- **AND** respond with `DeleteDownloadResult(true, null)`

#### Scenario: Delete unknown item
- **WHEN** a `DeleteDownload` is received for a DownloadId not in the Queued or Dispatched set
- **THEN** the Manager SHALL respond with `DeleteDownloadResult(false, "Item not found")`

### Requirement: DownloadManager handles retry
The DownloadManager SHALL handle `RetryDownload` messages by re-enqueuing a download and resetting the Worker.

#### Scenario: Retry download
- **WHEN** a `RetryDownload` is received with a DownloadId
- **THEN** the Manager SHALL persist a `DownloadEnqueued` event
- **AND** send `ResetDownload` to the Worker shard region
- **AND** call DispatchNext
- **AND** respond with `RetryDownloadResult(true, null)`

### Requirement: DownloadManager state
The DownloadManager SHALL maintain a persistent state containing two sets of DownloadIds: Queued and Dispatched. No download metadata, progress, or history SHALL be stored on the Manager.

#### Scenario: State structure
- **WHEN** the Manager state is inspected
- **THEN** the persistent state SHALL contain an ordered list of Queued DownloadIds and a set of Dispatched DownloadIds
- **AND** no other download data

### Requirement: DownloadManager persistence is T1 event-sourced
The DownloadManager SHALL persist state changes using Akka.Persistence event sourcing with three event types: `DownloadEnqueued`, `DownloadDispatched`, `DownloadDequeued`.

#### Scenario: Recovery after restart
- **WHEN** the DownloadManager recovers from a restart
- **THEN** the Queued and Dispatched sets SHALL be restored from persisted events
- **AND** items that were Dispatched at crash time SHALL be moved to Queued
- **AND** DispatchNext SHALL be called to re-dispatch StartDownload signals to Workers

## REMOVED Requirements

### Requirement: DownloadManager tracks progress
**Reason**: Progress tracking moves to the DownloadWorker. The Manager no longer holds a progress dictionary. Progress is retrieved on demand via `QueryWorkerStatus` fan-out.
**Migration**: Progress data is now returned as part of `WorkerStatusResult` when the Manager fans out `QueryWorkerStatus` during queue queries.

### Requirement: DownloadManager answers history queries
**Reason**: History queries are now served by the DownloadHistoryActor, a dedicated read-side projection. The Manager no longer stores or serves history data.
**Migration**: `QueryHistory` messages are sent to the `IDownloadHistory` actor instead of `IDownloadManager`.
