## MODIFIED Requirements

### Requirement: History query returns newest-first
The DownloadHistoryManager SHALL return history records in reverse chronological order (newest first) when responding to `QueryHistory`.

#### Scenario: History ordering
- **WHEN** a `QueryHistory` message is received with `start=0` and `limit=10`
- **AND** there are 50 history records
- **THEN** the response SHALL contain the 10 most recent records
- **AND** records SHALL be ordered newest-first

#### Scenario: History ordering with offset
- **WHEN** a `QueryHistory` message is received with `start=10` and `limit=10`
- **THEN** the response SHALL skip the 10 newest records and return the next 10 in reverse chronological order

#### Scenario: Append order preserved internally
- **WHEN** a new `RecordDownload` message is received
- **THEN** the record SHALL be appended to the internal list (no reordering of storage)
- **AND** only the query projection reverses the order
