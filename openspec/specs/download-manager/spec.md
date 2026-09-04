# Download Manager

## Purpose

Cluster Singleton actor managing download queue, concurrency limits, and persistence. Coordinates DownloadWorker shard region.

## Requirements

### Requirement: DownloadManager is a Cluster Singleton
The DownloadManager SHALL be registered as a Cluster Singleton actor named "download-manager".

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** exactly one DownloadManager instance SHALL exist in the cluster

### Requirement: DownloadManager accepts AddDownload
The DownloadManager SHALL handle `AddDownload` messages by assigning a new `Guid` as `DownloadId`, persisting a `DownloadEnqueued` event, forwarding an `InitDownload` message (with domain metadata only, no paths) to the DownloadWorker shard region, and responding with `DownloadAdded`.

#### Scenario: Successful add
- **WHEN** an `AddDownload` message is received
- **THEN** the Manager SHALL generate a new DownloadId
- **AND** persist a `DownloadEnqueued` event with DownloadId
- **AND** send `InitDownload` with domain metadata (DownloadId, Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category) to the Worker shard region
- **AND** the `InitDownload` message SHALL NOT contain IncompletePath or OutputPath
- **AND** respond with `DownloadAdded(DownloadId)`
- **AND** call DispatchNext to check if the download can start immediately

### Requirement: DownloadManager enforces concurrency limit
The DownloadManager SHALL limit the number of concurrent downloads to the configured `DownloadOptions.ConcurrentDownloads` (default 3). When a slot is available, the Manager SHALL persist a `DownloadDispatched` event and send a bare `StartDownload(DownloadId)` go-signal to the Worker shard region.

#### Scenario: Under capacity
- **WHEN** DispatchNext runs and fewer than `DownloadOptions.ConcurrentDownloads` downloads are in the Dispatched set
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
The DownloadManager SHALL handle `QueryQueue` messages by fanning out `QueryWorkerStatus` to all Workers in its Queued and Dispatched sets, collecting responses, and building a `QueueResult`. The handler SHALL apply the `Category` filter, `Start` offset, and `Limit` from the `QueryQueue` message to the collected responses before building the result.

#### Scenario: Queue query with fan-out
- **WHEN** a `QueryQueue` message is received
- **THEN** the Manager SHALL send `QueryWorkerStatus` to each Worker in the Queued and Dispatched sets via the shard region
- **AND** collect responses with a timeout of 2 seconds
- **AND** respond with a `QueueResult` built from Worker responses
- **AND** Workers that do not respond within the timeout SHALL be represented with zero progress

#### Scenario: Queue query with category filter
- **WHEN** a `QueryQueue` message is received with `Category = "sonarr"`
- **THEN** the Manager SHALL include only items matching the `"sonarr"` category in the result

#### Scenario: Queue query with pagination
- **WHEN** a `QueryQueue` message is received with `Start = 2` and `Limit = 5`
- **THEN** the Manager SHALL skip the first 2 items and return at most 5 items
- **AND** `QueueResult.TotalItems` SHALL reflect the total count after category filtering but before pagination

#### Scenario: Queue query with Limit 0 means all
- **WHEN** a `QueryQueue` message is received with `Limit = 0`
- **THEN** the Manager SHALL return all items (after category filter and start offset)

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

### Requirement: DownloadManager persistence path
The DownloadManager SHALL use `DataPaths.Database` for the Akka.Persistence SQLite database path instead of `FunkArrOptions.PersistencePath`.

#### Scenario: Persistence configuration
- **WHEN** the actor system configures Akka.Persistence
- **THEN** the SQLite connection string SHALL use `DataPaths.Database` as the database file path

### Requirement: DownloadManager persistence is T1 event-sourced
The DownloadManager SHALL persist state changes using Akka.Persistence event sourcing with three event types: `DownloadEnqueued`, `DownloadDispatched`, `DownloadDequeued`.

#### Scenario: Recovery after restart
- **WHEN** the DownloadManager recovers from a restart
- **THEN** the Queued and Dispatched sets SHALL be restored from persisted events
- **AND** items that were Dispatched at crash time SHALL be moved to Queued
- **AND** DispatchNext SHALL be called to re-dispatch StartDownload signals to Workers
