## MODIFIED Requirements

### Requirement: DownloadHistoryManager handles RecordDownload
The DownloadHistoryManager SHALL handle `RecordDownload` messages from Workers by persisting a `HistoryRecorded` event (with `RelativePath` instead of `FilePath`) and updating its in-memory state.

#### Scenario: Record completed download
- **WHEN** a `RecordDownload` message is received with Completed status
- **THEN** the HistoryManager SHALL persist a `HistoryRecorded` event with DownloadId, Title, Category, Size, Status, RelativePath, DownloadTimeSeconds, and CompletedAt
- **AND** add the record to its in-memory history list

#### Scenario: Record failed download
- **WHEN** a `RecordDownload` message is received with Failed status
- **THEN** the HistoryManager SHALL persist a `HistoryRecorded` event with DownloadId, Title, Category, Size, Status, null RelativePath, FailMessage, and CompletedAt
- **AND** add the record to its in-memory history list

#### Scenario: Duplicate record
- **WHEN** a `RecordDownload` message is received for a DownloadId that already exists in the history
- **THEN** the HistoryManager SHALL ignore the message

### Requirement: DownloadHistoryManager state
The DownloadHistoryManager SHALL maintain a persistent state containing a list of history records with `RelativePath` instead of `FilePath`.

#### Scenario: State structure
- **WHEN** the HistoryManager state is inspected
- **THEN** the state SHALL contain a list of HistoryRecord entries (DownloadId, Title, Category, Size, Status, RelativePath?, FailMessage?, DownloadTimeSeconds, CompletedAt)

### Requirement: DownloadHistoryManager answers history queries
The DownloadHistoryManager SHALL handle `QueryHistory` messages by applying filters and responding with `HistoryResult` containing `RelativePath` instead of `FilePath`.

#### Scenario: History query
- **WHEN** a `QueryHistory` message is received with default parameters
- **THEN** the HistoryManager SHALL respond with a `HistoryResult` containing all items, each with `RelativePath` instead of `FilePath`
