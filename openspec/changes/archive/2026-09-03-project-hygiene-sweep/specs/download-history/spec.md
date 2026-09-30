## MODIFIED Requirements

### Requirement: DownloadHistoryActor is a Cluster Singleton
The DownloadHistoryManager SHALL be registered as a Cluster Singleton actor named "download-history" using `resolver.Props<DownloadHistoryManager>()`.

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** exactly one DownloadHistoryManager instance SHALL exist in the cluster

### Requirement: DownloadHistoryActor answers history queries
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

## RENAMED Requirements

### Requirement: DownloadHistoryActor is a Cluster Singleton
- **FROM:** DownloadHistoryActor
- **TO:** DownloadHistoryManager

### Requirement: DownloadHistoryActor handles RecordDownload
- **FROM:** DownloadHistoryActor handles RecordDownload
- **TO:** DownloadHistoryManager handles RecordDownload

### Requirement: DownloadHistoryActor handles RemoveHistoryEntry
- **FROM:** DownloadHistoryActor handles RemoveHistoryEntry
- **TO:** DownloadHistoryManager handles RemoveHistoryEntry

### Requirement: DownloadHistoryActor answers history queries
- **FROM:** DownloadHistoryActor answers history queries
- **TO:** DownloadHistoryManager answers history queries

### Requirement: DownloadHistoryActor state
- **FROM:** DownloadHistoryActor state
- **TO:** DownloadHistoryManager state

### Requirement: DownloadHistoryActor persistence is T2 event-sourced
- **FROM:** DownloadHistoryActor persistence is T2 event-sourced
- **TO:** DownloadHistoryManager persistence is T2 event-sourced

### Requirement: IDownloadHistory marker interface
- **FROM:** IDownloadHistory marker interface
- **TO:** IDownloadHistoryManager marker interface
