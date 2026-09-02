# Download Manager

## Purpose

Cluster Singleton actor managing download queue, history, concurrency limits, and persistence. Coordinates DownloadWorker shard region.

## Requirements

### Requirement: DownloadManager is a Cluster Singleton
The DownloadManager SHALL be registered as a Cluster Singleton actor named "download-manager".

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** exactly one DownloadManager instance SHALL exist in the cluster

### Requirement: DownloadManager accepts AddDownload
The DownloadManager SHALL handle `AddDownload` messages by assigning a new `Guid` as `DownloadId`, persisting the download entry, and responding with `DownloadAdded`.

#### Scenario: Successful add
- **WHEN** an `AddDownload` message is received
- **THEN** the Manager SHALL generate a new DownloadId, add the item to the queue with status `Queued`, persist the state change, and respond with `DownloadAdded(DownloadId)`

### Requirement: DownloadManager enforces concurrency limit
The DownloadManager SHALL limit the number of concurrent downloads to a configurable maximum (default 3). Downloads beyond the limit SHALL be stashed and processed as slots free up.

#### Scenario: Under capacity
- **WHEN** an AddDownload arrives and fewer than the configured maximum downloads are in Processing status
- **THEN** the Manager SHALL immediately send StartDownload to the DownloadWorker shard region

#### Scenario: At capacity
- **WHEN** an AddDownload arrives and the maximum number of downloads are already Processing
- **THEN** the Manager SHALL add the item to queue with status Queued and stash the dispatch

#### Scenario: Slot freed
- **WHEN** a DownloadCompleted or DownloadFailed is received
- **THEN** the Manager SHALL start the next Queued download if any exist

### Requirement: DownloadManager tracks progress
The DownloadManager SHALL handle `DownloadProgress` messages from workers and update the in-memory state for the corresponding queue item. Progress SHALL NOT be persisted.

#### Scenario: Progress update
- **WHEN** a DownloadProgress message is received for a known DownloadId
- **THEN** the Manager SHALL update the in-memory queue item with current BytesDownloaded, CurrentTimeUs, and Speed

#### Scenario: Progress for unknown download
- **WHEN** a DownloadProgress message is received for an unknown DownloadId
- **THEN** the Manager SHALL ignore the message

### Requirement: DownloadManager handles completion
The DownloadManager SHALL handle `DownloadCompleted` messages by moving the item from queue to history with status `Completed` and persisting the state change.

#### Scenario: Download completed
- **WHEN** a DownloadCompleted message is received
- **THEN** the Manager SHALL remove the item from the queue, add it to history with status Completed, FilePath, DownloadTimeSeconds, and CompletedAt timestamp, and persist the change

### Requirement: DownloadManager handles failure
The DownloadManager SHALL handle `DownloadFailed` messages by moving the item from queue to history with status `Failed` and persisting the state change.

#### Scenario: Download failed
- **WHEN** a DownloadFailed message is received
- **THEN** the Manager SHALL remove the item from the queue, add it to history with status Failed and the failure reason, and persist the change

### Requirement: DownloadManager answers queue queries
The DownloadManager SHALL handle `QueryQueue` messages by responding with a `QueueResult` reflecting current queue state.

#### Scenario: Queue query
- **WHEN** a QueryQueue message is received
- **THEN** the Manager SHALL respond with a QueueResult containing all items with status Queued or Processing, including latest progress data

### Requirement: DownloadManager answers history queries
The DownloadManager SHALL handle `QueryHistory` messages by responding with a `HistoryResult` reflecting download history.

#### Scenario: History query
- **WHEN** a QueryHistory message is received
- **THEN** the Manager SHALL respond with a HistoryResult containing all Completed and Failed items

### Requirement: DownloadManager handles delete
The DownloadManager SHALL handle `DeleteDownload` messages by removing items from queue or history.

#### Scenario: Delete from queue
- **WHEN** a DeleteDownload is received for an item in the queue
- **THEN** the Manager SHALL remove it from the queue, persist the change, and respond with `DeleteDownloadResult(true, null)`

#### Scenario: Delete from history
- **WHEN** a DeleteDownload is received for an item in history
- **THEN** the Manager SHALL remove it from history, persist the change, and respond with `DeleteDownloadResult(true, null)`

#### Scenario: Delete unknown item
- **WHEN** a DeleteDownload is received for an unknown DownloadId
- **THEN** the Manager SHALL respond with `DeleteDownloadResult(false, "Item not found")`

### Requirement: DownloadManager handles retry
The DownloadManager SHALL handle `RetryDownload` messages by re-queuing a failed download from history.

#### Scenario: Retry failed item
- **WHEN** a RetryDownload is received for a Failed item in history
- **THEN** the Manager SHALL remove it from history, add it back to queue with status Queued, persist the change, and respond with `RetryDownloadResult(true, null)`

#### Scenario: Retry non-failed item
- **WHEN** a RetryDownload is received for a Completed item in history
- **THEN** the Manager SHALL respond with `RetryDownloadResult(false, "Item is not failed")`

#### Scenario: Retry unknown item
- **WHEN** a RetryDownload is received for an unknown DownloadId
- **THEN** the Manager SHALL respond with `RetryDownloadResult(false, "Item not found")`

### Requirement: DownloadManager persistence is T1 event-sourced
The DownloadManager SHALL persist queue and history state changes using Akka.Persistence event sourcing. On recovery, the full queue and history state SHALL be rebuilt from the journal.

#### Scenario: Recovery after restart
- **WHEN** the DownloadManager recovers from a restart
- **THEN** all previously persisted queue and history items SHALL be restored
- **AND** items that were Processing at crash time SHALL be re-queued with status Queued

### Requirement: DownloadManager state
The DownloadManager SHALL maintain an explicit state record containing the download queue and history.

#### Scenario: State structure
- **WHEN** the Manager state is inspected
- **THEN** it SHALL contain a list of queue entries (DownloadId, metadata, status, progress) and a list of history entries (DownloadId, metadata, status, result)
