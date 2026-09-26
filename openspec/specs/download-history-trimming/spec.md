# Download History Trimming

## Purpose

Bounded history with FIFO trimming, pre-aggregated stats, and indexed lookups for the DownloadHistoryManager.

## Requirements

### Requirement: DownloadHistoryManager trims records to MaxHistoryRecords
The DownloadHistoryManager SHALL trim its in-memory record list to `MaxHistoryRecords` (from DownloadOptions) after adding new records. Trimming SHALL remove the oldest records first (FIFO).

#### Scenario: Trim after exceeding max
- **WHEN** a `HistoryRecorded` event is applied and the record count exceeds MaxHistoryRecords
- **THEN** the oldest records SHALL be removed until the count equals MaxHistoryRecords
- **AND** a `HistoryTrimmed(int Count)` event SHALL be persisted

#### Scenario: No trim when under max
- **WHEN** a `HistoryRecorded` event is applied and the record count is at or below MaxHistoryRecords
- **THEN** no trimming SHALL occur

#### Scenario: Default max records
- **WHEN** MaxHistoryRecords is not configured
- **THEN** the default value SHALL be 1000

### Requirement: DownloadHistoryManager pre-aggregates stats
The DownloadHistoryManagerState SHALL maintain running stat counters that update inline when records are added or removed, instead of recomputing from the full list on each query.

#### Scenario: Stats updated on record add
- **WHEN** a `HistoryRecorded` event is applied with status Completed
- **THEN** `TotalCompleted` SHALL increment by 1
- **AND** `TotalBytes` SHALL increase by the record's Size
- **AND** `TotalDownloadTimeSeconds` SHALL increase by the record's DownloadTimeSeconds

#### Scenario: Stats updated on failed record add
- **WHEN** a `HistoryRecorded` event is applied with status Failed
- **THEN** `TotalFailed` SHALL increment by 1

#### Scenario: Stats updated on record remove
- **WHEN** a `HistoryRemoved` event is applied for a Completed record
- **THEN** `TotalCompleted` SHALL decrement by 1
- **AND** `TotalBytes` SHALL decrease by the record's Size
- **AND** `TotalDownloadTimeSeconds` SHALL decrease by the record's DownloadTimeSeconds

#### Scenario: Stats updated on trim
- **WHEN** records are trimmed
- **THEN** the running counters SHALL be decremented for each trimmed record

#### Scenario: QueryHistoryStats uses pre-aggregated counters
- **WHEN** a `QueryHistoryStats` message is received
- **THEN** the response SHALL be computed from the running counters, not by iterating all records

### Requirement: DownloadHistoryManager uses Dictionary index for Contains
The DownloadHistoryManagerState SHALL maintain a `HashSet<Guid>` for O(1) duplicate detection instead of `Records.Any(r => r.DownloadId == id)`.

#### Scenario: Contains check is O(1)
- **WHEN** `Contains(downloadId)` is called
- **THEN** it SHALL use the HashSet, not iterate the list

#### Scenario: Index maintained on add
- **WHEN** a record is added
- **THEN** the DownloadId SHALL be added to the HashSet

#### Scenario: Index maintained on remove
- **WHEN** a record is removed
- **THEN** the DownloadId SHALL be removed from the HashSet

#### Scenario: Index maintained on trim
- **WHEN** records are trimmed
- **THEN** the trimmed DownloadIds SHALL be removed from the HashSet
