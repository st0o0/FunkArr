## ADDED Requirements

### Requirement: DownloadHistoryActor is a Cluster Singleton
The DownloadHistoryActor SHALL be registered as a Cluster Singleton actor named "download-history".

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** exactly one DownloadHistoryActor instance SHALL exist in the cluster

### Requirement: DownloadHistoryActor handles RecordDownload
The DownloadHistoryActor SHALL handle `RecordDownload` messages from Workers by persisting a `HistoryRecorded` event and updating its in-memory state.

#### Scenario: Record completed download
- **WHEN** a `RecordDownload` message is received with Completed status
- **THEN** the HistoryActor SHALL persist a `HistoryRecorded` event with DownloadId, Title, Category, Size, Status, FilePath, DownloadTimeSeconds, and CompletedAt
- **AND** add the record to its in-memory history list

#### Scenario: Record failed download
- **WHEN** a `RecordDownload` message is received with Failed status
- **THEN** the HistoryActor SHALL persist a `HistoryRecorded` event with DownloadId, Title, Category, Size, Status, FailMessage, and CompletedAt
- **AND** add the record to its in-memory history list

#### Scenario: Duplicate record
- **WHEN** a `RecordDownload` message is received for a DownloadId that already exists in the history
- **THEN** the HistoryActor SHALL ignore the message

### Requirement: DownloadHistoryActor handles RemoveHistoryEntry
The DownloadHistoryActor SHALL handle `RemoveHistoryEntry` messages by persisting a `HistoryRemoved` event and removing the entry from its in-memory state.

#### Scenario: Remove existing entry
- **WHEN** a `RemoveHistoryEntry` message is received for a known DownloadId
- **THEN** the HistoryActor SHALL persist a `HistoryRemoved` event
- **AND** remove the record from its in-memory history list
- **AND** respond with `DeleteDownloadResult(true, null)`

#### Scenario: Remove unknown entry
- **WHEN** a `RemoveHistoryEntry` message is received for an unknown DownloadId
- **THEN** the HistoryActor SHALL respond with `DeleteDownloadResult(false, "Item not found")`

### Requirement: DownloadHistoryActor answers history queries
The DownloadHistoryActor SHALL handle `QueryHistory` messages by responding with a `HistoryResult` from its in-memory state.

#### Scenario: History query
- **WHEN** a `QueryHistory` message is received
- **THEN** the HistoryActor SHALL respond with a `HistoryResult` containing all Completed and Failed items from its in-memory state

#### Scenario: History query with pagination
- **WHEN** a `QueryHistory` message is received with Start and Limit parameters
- **THEN** the HistoryActor SHALL respond with the appropriate page of history items

#### Scenario: History query with category filter
- **WHEN** a `QueryHistory` message is received with a Category filter
- **THEN** the HistoryActor SHALL respond with only items matching the specified Category

### Requirement: DownloadHistoryActor state
The DownloadHistoryActor SHALL maintain a persistent state containing a list of history records.

#### Scenario: State structure
- **WHEN** the HistoryActor state is inspected
- **THEN** the state SHALL contain a list of HistoryRecord entries (DownloadId, Title, Category, Size, Status, FilePath?, FailMessage?, DownloadTimeSeconds?, CompletedAt)

### Requirement: DownloadHistoryActor persistence is T2 event-sourced
The DownloadHistoryActor SHALL persist state changes using Akka.Persistence event sourcing with two event types: `HistoryRecorded` and `HistoryRemoved`.

#### Scenario: Recovery after restart
- **WHEN** the DownloadHistoryActor recovers from a restart
- **THEN** all previously persisted history records SHALL be restored from the journal
- **AND** the in-memory history list SHALL be immediately available for queries
