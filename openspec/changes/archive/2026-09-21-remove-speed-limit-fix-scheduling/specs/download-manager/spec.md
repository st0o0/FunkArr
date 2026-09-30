## MODIFIED Requirements

### Requirement: DownloadManager enforces concurrency limit
The DownloadManager SHALL limit the number of concurrent downloads to the configured `DownloadOptions.ConcurrentDownloads` (default 3). The DownloadManager SHALL obtain the current time via an injected `TimeProvider` (not `DateTime.Now`). When a slot is available AND the current server-local time is within a configured download schedule (or no schedule is configured), the Manager SHALL persist a `DownloadDispatched` event and send a bare `StartDownload(DownloadId)` go-signal to the Worker shard region. When outside all scheduled windows, the Manager SHALL schedule a timer for the next window start instead of dispatching.

#### Scenario: Under capacity
- **WHEN** DispatchNext runs and fewer than `DownloadOptions.ConcurrentDownloads` downloads are in the Dispatched set
- **AND** the current time is within a configured schedule window (or no schedule is configured)
- **THEN** the Manager SHALL move the next Queued item to the Dispatched set
- **AND** persist a `DownloadDispatched` event with DownloadId
- **AND** send `StartDownload(DownloadId)` to the Worker shard region

#### Scenario: Under capacity but outside schedule
- **WHEN** DispatchNext runs and slots are available
- **AND** a schedule is configured and the current time is outside all windows
- **THEN** the Manager SHALL NOT dispatch any downloads
- **AND** SHALL schedule a timer for the next window start

#### Scenario: At capacity
- **WHEN** DispatchNext runs and the Dispatched set has reached the configured maximum
- **THEN** the Manager SHALL not dispatch any further downloads

#### Scenario: Slot freed
- **WHEN** a `SlotFree` message is received
- **THEN** the Manager SHALL persist a `DownloadDequeued` event for the DownloadId
- **AND** call DispatchNext

#### Scenario: TimeProvider injection
- **WHEN** the DownloadManager is constructed
- **THEN** it SHALL receive `TimeProvider` via constructor dependency injection
- **AND** use `TimeProvider.GetLocalNow()` to determine the current time in `DispatchNext()`
