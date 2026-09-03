# Download History

## Purpose

Cluster Singleton actor serving as a read-side projection for completed and failed downloads. Receives records from Workers and answers history queries.

## Requirements

### Requirement: DownloadHistoryManager is a Cluster Singleton
The DownloadHistoryManager SHALL be registered as a Cluster Singleton actor named "download-history" using `resolver.Props<DownloadHistoryManager>()`.

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** exactly one DownloadHistoryManager instance SHALL exist in the cluster

### Requirement: DownloadHistoryManager handles RecordDownload
The DownloadHistoryManager SHALL handle `RecordDownload` messages from Workers by persisting a `HistoryRecorded` event and updating its in-memory state.

#### Scenario: Record completed download
- **WHEN** a `RecordDownload` message is received with Completed status
- **THEN** the HistoryManager SHALL persist a `HistoryRecorded` event with DownloadId, Title, Category, Size, Status, FilePath, DownloadTimeSeconds, and CompletedAt
- **AND** add the record to its in-memory history list

#### Scenario: Record failed download
- **WHEN** a `RecordDownload` message is received with Failed status
- **THEN** the HistoryManager SHALL persist a `HistoryRecorded` event with DownloadId, Title, Category, Size, Status, FailMessage, and CompletedAt
- **AND** add the record to its in-memory history list

#### Scenario: Duplicate record
- **WHEN** a `RecordDownload` message is received for a DownloadId that already exists in the history
- **THEN** the HistoryManager SHALL ignore the message

### Requirement: DownloadHistoryManager handles RemoveHistoryEntry
The DownloadHistoryManager SHALL handle `RemoveHistoryEntry` messages by persisting a `HistoryRemoved` event and removing the entry from its in-memory state.

#### Scenario: Remove existing entry
- **WHEN** a `RemoveHistoryEntry` message is received for a known DownloadId
- **THEN** the HistoryManager SHALL persist a `HistoryRemoved` event
- **AND** remove the record from its in-memory history list
- **AND** respond with `DeleteDownloadResult(true, null)`

#### Scenario: Remove unknown entry
- **WHEN** a `RemoveHistoryEntry` message is received for an unknown DownloadId
- **THEN** the HistoryManager SHALL respond with `DeleteDownloadResult(false, "Item not found")`

### Requirement: DownloadHistoryManager answers history queries
The DownloadHistoryManager SHALL handle `QueryHistory` messages by applying `Category` filter, `Start` offset, and `Limit` from the message to its in-memory state before responding with a `HistoryResult`.

#### Scenario: History query
- **WHEN** a `QueryHistory` message is received with default parameters
- **THEN** the HistoryManager SHALL respond with a `HistoryResult` containing all Completed and Failed items from its in-memory state

#### Scenario: History query with pagination
- **WHEN** a `QueryHistory` message is received with `Start = 10` and `Limit = 25`
- **THEN** the HistoryManager SHALL skip the first 10 items and return at most 25 items
- **AND** `HistoryResult.TotalItems` SHALL reflect the total count after category filtering but before pagination

#### Scenario: History query with category filter
- **WHEN** a `QueryHistory` message is received with `Category = "sonarr"`
- **THEN** the HistoryManager SHALL respond with only items matching the `"sonarr"` category

#### Scenario: History query with Limit 0 means all
- **WHEN** a `QueryHistory` message is received with `Limit = 0`
- **THEN** the HistoryManager SHALL return all items (after category filter and start offset)

### Requirement: DownloadHistoryManager state
The DownloadHistoryManager SHALL maintain a persistent state containing a list of history records.

#### Scenario: State structure
- **WHEN** the HistoryManager state is inspected
- **THEN** the state SHALL contain a list of HistoryRecord entries (DownloadId, Title, Category, Size, Status, FilePath?, FailMessage?, DownloadTimeSeconds?, CompletedAt)

### Requirement: DownloadHistoryManager persistence is T2 event-sourced
The DownloadHistoryManager SHALL persist state changes using Akka.Persistence event sourcing with two event types: `HistoryRecorded` and `HistoryRemoved`.

#### Scenario: Recovery after restart
- **WHEN** the DownloadHistoryManager recovers from a restart
- **THEN** all previously persisted history records SHALL be restored from the journal
- **AND** the in-memory history list SHALL be immediately available for queries
