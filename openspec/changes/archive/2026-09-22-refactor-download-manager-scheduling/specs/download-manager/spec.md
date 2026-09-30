## MODIFIED Requirements

### Requirement: DownloadManager enforces concurrency limit
The DownloadManager SHALL limit the number of concurrent downloads to the configured `DownloadOptions.ConcurrentDownloads` (default 3). The DownloadManager SHALL use a two-gate dispatch model: dispatching only proceeds when both `ScheduleEnabled` is true (set by DownloadScheduler) AND `Paused` is false (set by user). When a slot is available and both gates are open, the Manager SHALL persist a `DownloadDispatched` event and send a bare `StartDownload(DownloadId)` go-signal to the Worker shard region. The Manager SHALL NOT own any time-window logic, timer management, or TimeProvider dependency.

#### Scenario: Under capacity with both gates open
- **WHEN** DispatchNext runs and fewer than `DownloadOptions.ConcurrentDownloads` downloads are in the Dispatched set
- **AND** `Paused` is false and `ScheduleEnabled` is true
- **THEN** the Manager SHALL move the next Queued item to the Dispatched set
- **AND** persist a `DownloadDispatched` event with DownloadId
- **AND** send `StartDownload(DownloadId)` to the Worker shard region

#### Scenario: Under capacity but schedule gate closed
- **WHEN** DispatchNext runs and slots are available
- **AND** `ScheduleEnabled` is false
- **THEN** the Manager SHALL NOT dispatch any downloads
- **AND** SHALL NOT manage any timers (timer management belongs to DownloadScheduler)

#### Scenario: At capacity
- **WHEN** DispatchNext runs and the Dispatched set has reached the configured maximum
- **THEN** the Manager SHALL not dispatch any further downloads

#### Scenario: Slot freed
- **WHEN** a `SlotFree` message is received
- **THEN** the Manager SHALL persist a `DownloadDequeued` event for the DownloadId
- **AND** call DispatchNext

### Requirement: DownloadManager handles ScheduleEnabled
The DownloadManager SHALL handle `ScheduleEnabled` messages from the DownloadScheduler by setting `ScheduleEnabled = true`, clearing `NextWindow`, and calling `DispatchNext()`.

#### Scenario: Schedule enabled
- **WHEN** a `ScheduleEnabled` message is received
- **THEN** the Manager SHALL set `ScheduleEnabled = true`
- **AND** set `NextWindow = null`
- **AND** call `DispatchNext()`

### Requirement: DownloadManager handles ScheduleDisabled
The DownloadManager SHALL handle `ScheduleDisabled(DateTimeOffset? NextWindow)` messages from the DownloadScheduler by setting `ScheduleEnabled = false` and storing the next window time.

#### Scenario: Schedule disabled with next window
- **WHEN** a `ScheduleDisabled(NextWindow: 2026-09-22T23:00:00+02:00)` message is received
- **THEN** the Manager SHALL set `ScheduleEnabled = false`
- **AND** set `NextWindow = 2026-09-22T23:00:00+02:00`

### Requirement: DownloadManager state includes gate fields
The DownloadManager state record SHALL include `Paused` (bool, default false), `ScheduleEnabled` (bool, default true), and `NextWindow` (DateTimeOffset?, default null) alongside the existing Queued and Dispatched fields.

#### Scenario: Default state
- **WHEN** the Manager starts with an empty journal
- **THEN** `Paused` SHALL be false
- **AND** `ScheduleEnabled` SHALL be true
- **AND** `NextWindow` SHALL be null

### Requirement: DownloadManager answers queue queries with pipeline status
The DownloadManager SHALL include `IsPaused`, `IsScheduleActive`, and `NextWindow` in the `QueueResult` response.

#### Scenario: Queue query includes pipeline status
- **WHEN** a `QueryQueue` message is received
- **AND** the Manager is paused and schedule is disabled with NextWindow = 23:00
- **THEN** the `QueueResult` SHALL include `IsPaused = true`, `IsScheduleActive = false`, `NextWindow = 23:00`

### Requirement: DownloadManager does not depend on TimeProvider
The DownloadManager SHALL NOT inject or depend on `TimeProvider`. All time-related scheduling decisions are made by the DownloadScheduler.

#### Scenario: Constructor signature
- **WHEN** the DownloadManager is constructed
- **THEN** it SHALL accept `IOptionsMonitor<DownloadOptions>` but NOT `TimeProvider`

## REMOVED Requirements

### Requirement: DownloadManager checks time-window before dispatching (from download-scheduling spec)
**Reason**: Time-window checking responsibility moves to the new DownloadScheduler actor. The Manager receives Enable/Disable signals instead of checking time windows directly.
**Migration**: The DownloadScheduler actor evaluates time windows and sends ScheduleEnabled/ScheduleDisabled to the Manager.

### Requirement: Schedule timer management (from download-scheduling spec)
**Reason**: Timer lifecycle moves to the DownloadScheduler actor. The Manager no longer owns `_scheduleTimer`, `ScheduleWake`, or `ICancelable`.
**Migration**: The DownloadScheduler manages its own timers for schedule boundary transitions.
