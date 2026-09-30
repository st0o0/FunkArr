## MODIFIED Requirements

### Requirement: DownloadManager accepts AddDownload
The DownloadManager SHALL handle `AddDownload` messages by assigning a new `Guid` as `DownloadId`, persisting a `DownloadRegistered` event with display metadata, forwarding an `InitDownload` message to the DownloadWorker shard region, and responding with `DownloadAdded`.

#### Scenario: Successful add
- **WHEN** an `AddDownload` message is received
- **THEN** the Manager SHALL generate a new DownloadId
- **AND** persist a `DownloadRegistered` event with DownloadId, Title, Category, Size
- **AND** send `InitDownload` (with all metadata: DownloadId, Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category, OutputPath) to the Worker shard region
- **AND** respond with `DownloadAdded(DownloadId)`
- **AND** call DispatchNext to check if the download can start immediately

### Requirement: DownloadManager enforces concurrency limit
The DownloadManager SHALL limit the number of concurrent downloads to a configurable maximum (default 3). When a slot is available, the Manager SHALL send a bare `StartDownload(DownloadId)` go-signal to the Worker shard region.

#### Scenario: Under capacity
- **WHEN** DispatchNext runs and fewer than the configured maximum downloads are in Processing status
- **THEN** the Manager SHALL persist a `DownloadStatusChanged` event moving the next Queued item to Processing
- **AND** send `StartDownload(DownloadId)` (no payload beyond the ID) to the Worker shard region

#### Scenario: At capacity
- **WHEN** DispatchNext runs and the maximum number of downloads are already Processing
- **THEN** the Manager SHALL not dispatch any further downloads

#### Scenario: Slot freed
- **WHEN** a DownloadCompleted or DownloadFailed is received
- **THEN** the Manager SHALL persist the status change and call DispatchNext

### Requirement: DownloadManager tracks progress
The DownloadManager SHALL handle `DownloadProgress` messages from Workers and store progress in a non-persistent `Dictionary<Guid, ProgressInfo>`. Progress SHALL NOT be part of the persistent state or the DownloadRecord.

#### Scenario: Progress update
- **WHEN** a DownloadProgress message is received for a known DownloadId
- **THEN** the Manager SHALL update the in-memory progress dictionary with current BytesDownloaded, CurrentTimeUs, and Speed

#### Scenario: Progress for unknown download
- **WHEN** a DownloadProgress message is received for an unknown DownloadId
- **THEN** the Manager SHALL ignore the message

#### Scenario: Progress cleanup on completion
- **WHEN** a DownloadCompleted or DownloadFailed message is received
- **THEN** the Manager SHALL remove the entry from the progress dictionary

### Requirement: DownloadManager answers queue queries
The DownloadManager SHALL handle `QueryQueue` messages by responding with a `QueueResult` that merges persistent records with the non-persistent progress dictionary.

#### Scenario: Queue query with progress merge
- **WHEN** a QueryQueue message is received
- **THEN** the Manager SHALL respond with a QueueResult containing all items with status Queued or Processing
- **AND** for Processing items, merge BytesDownloaded, CurrentTimeUs, and Speed from the progress dictionary (defaulting to zero if no progress entry exists)

### Requirement: DownloadManager handles delete
The DownloadManager SHALL handle `DeleteDownload` messages by removing the record and sending `CancelDownload` to the Worker shard region.

#### Scenario: Delete active download
- **WHEN** a DeleteDownload is received for an item with status Queued or Processing
- **THEN** the Manager SHALL persist a `DownloadRemoved` event, send `CancelDownload` to the Worker shard region, remove progress from the dict, and respond with `DeleteDownloadResult(true, null)`

#### Scenario: Delete from history
- **WHEN** a DeleteDownload is received for an item with status Completed or Failed
- **THEN** the Manager SHALL persist a `DownloadRemoved` event and respond with `DeleteDownloadResult(true, null)`

#### Scenario: Delete unknown item
- **WHEN** a DeleteDownload is received for an unknown DownloadId
- **THEN** the Manager SHALL respond with `DeleteDownloadResult(false, "Item not found")`

### Requirement: DownloadManager handles retry
The DownloadManager SHALL handle `RetryDownload` messages by resetting a Failed download record to Queued and telling the Worker to reset.

#### Scenario: Retry failed item
- **WHEN** a RetryDownload is received for a Failed item
- **THEN** the Manager SHALL persist a `DownloadStatusChanged` event changing status from Failed to Queued
- **AND** send `ResetDownload` to the Worker shard region
- **AND** call DispatchNext
- **AND** respond with `RetryDownloadResult(true, null)`

#### Scenario: Retry non-failed item
- **WHEN** a RetryDownload is received for an item that is not Failed
- **THEN** the Manager SHALL respond with `RetryDownloadResult(false, "Item is not failed")`

#### Scenario: Retry unknown item
- **WHEN** a RetryDownload is received for an unknown DownloadId
- **THEN** the Manager SHALL respond with `RetryDownloadResult(false, "Item not found")`

### Requirement: DownloadManager state
The DownloadManager SHALL maintain a persistent state containing a flat list of `DownloadRecord` entries and a non-persistent progress dictionary.

#### Scenario: State structure
- **WHEN** the Manager state is inspected
- **THEN** the persistent state SHALL contain a list of DownloadRecord (DownloadId, Title, Category, Size, Status, FilePath?, FailMessage?, CompletedAt?, DownloadTimeSeconds?)
- **AND** a non-persistent Dictionary<Guid, ProgressInfo> for live progress data

### Requirement: DownloadManager persistence is T1 event-sourced
The DownloadManager SHALL persist state changes using Akka.Persistence event sourcing with three event types: `DownloadRegistered`, `DownloadStatusChanged`, `DownloadRemoved`.

#### Scenario: Recovery after restart
- **WHEN** the DownloadManager recovers from a restart
- **THEN** all previously persisted download records SHALL be restored
- **AND** items that were Processing at crash time SHALL be reset to Queued
- **AND** the progress dictionary SHALL be empty
- **AND** DispatchNext SHALL be called to re-dispatch StartDownload signals to Workers

## REMOVED Requirements

### Requirement: DownloadManager handles completion
**Reason**: Completion handling is still present but simplified — the Manager persists a `DownloadStatusChanged` event on receiving `DownloadCompleted`. The old requirement described moving items between Queue and History lists, which no longer exist as separate collections.
**Migration**: Covered by the modified `DownloadManager enforces concurrency limit` (slot freed scenario) and the flat record model in `DownloadManager state`.

### Requirement: DownloadManager handles failure
**Reason**: Same as completion — failure handling persists a status change on the flat record list, not a move between lists.
**Migration**: Covered by the modified `DownloadManager enforces concurrency limit` (slot freed scenario) and the flat record model.
