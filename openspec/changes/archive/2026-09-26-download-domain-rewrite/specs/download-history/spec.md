# Download History (Delta)

## MODIFIED Requirements

### Requirement: DownloadHistoryManager handles RecordDownload
The DownloadHistoryManager SHALL handle `RecordDownload` messages from Workers by persisting a `HistoryRecorded` event, updating its in-memory state, and trimming if the record count exceeds MaxHistoryRecords.

#### Scenario: Record completed download
- **WHEN** a `RecordDownload` message is received with Completed status
- **THEN** the HistoryManager SHALL persist a `HistoryRecorded` event
- **AND** add the record to its in-memory history list
- **AND** update pre-aggregated stats (increment TotalCompleted, add to TotalBytes and TotalDownloadTimeSeconds)
- **AND** trim oldest records if count exceeds MaxHistoryRecords

#### Scenario: Record failed download
- **WHEN** a `RecordDownload` message is received with Failed status
- **THEN** the HistoryManager SHALL persist a `HistoryRecorded` event
- **AND** add the record to its in-memory history list
- **AND** update pre-aggregated stats (increment TotalFailed)
- **AND** trim oldest records if count exceeds MaxHistoryRecords

#### Scenario: Duplicate record
- **WHEN** a `RecordDownload` message is received for a DownloadId that already exists in the history
- **THEN** the HistoryManager SHALL ignore the message

### Requirement: DownloadHistoryManager handles RemoveHistoryEntry
The DownloadHistoryManager SHALL handle `RemoveHistoryEntry` messages by persisting a `HistoryRemoved` event, removing the entry from its in-memory state, and updating pre-aggregated stats.

#### Scenario: Remove existing entry
- **WHEN** a `RemoveHistoryEntry` message is received for a known DownloadId
- **THEN** the HistoryManager SHALL persist a `HistoryRemoved` event
- **AND** remove the record from its in-memory history list
- **AND** remove the DownloadId from the HashSet index
- **AND** update pre-aggregated stats (decrement appropriate counters)
- **AND** respond with `DeleteDownloadResult(true, null)`

#### Scenario: Remove unknown entry
- **WHEN** a `RemoveHistoryEntry` message is received for an unknown DownloadId
- **THEN** the HistoryManager SHALL respond with `DeleteDownloadResult(false, "Item not found")`

### Requirement: DownloadHistoryManager handles QueryHistoryStats
The DownloadHistoryManager SHALL handle `QueryHistoryStats` messages by responding with pre-aggregated stats from running counters.

#### Scenario: Stats with records
- **WHEN** a `QueryHistoryStats` message is received and history records exist
- **THEN** the HistoryManager SHALL respond with `HistoryStatsResult` computed from running counters: `TotalCompleted`, `TotalFailed`, `TotalBytes`, `AverageDownloadTimeSeconds` (TotalDownloadTimeSeconds / TotalCompleted), `SuccessRate` (TotalCompleted / (TotalCompleted + TotalFailed))

#### Scenario: Stats with no records
- **WHEN** a `QueryHistoryStats` message is received and no history records exist
- **THEN** the HistoryManager SHALL respond with all values as 0

### Requirement: DownloadHistoryManager state
The DownloadHistoryManagerState SHALL maintain a list of history records, a HashSet index for O(1) Contains checks, and pre-aggregated stat counters.

#### Scenario: State structure
- **WHEN** the HistoryManager state is inspected
- **THEN** the state SHALL contain a list of HistoryRecord entries, a `HashSet<Guid>` index, and running counters (TotalCompleted, TotalFailed, TotalBytes, TotalDownloadTimeSeconds)
