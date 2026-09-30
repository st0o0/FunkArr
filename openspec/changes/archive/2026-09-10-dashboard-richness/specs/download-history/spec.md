## ADDED Requirements

### Requirement: DownloadHistoryManager handles QueryHistoryStats
The DownloadHistoryManager SHALL handle `QueryHistoryStats` messages by computing aggregate statistics from its in-memory state and responding with `HistoryStatsResult`.

#### Scenario: Stats with records
- **WHEN** a `QueryHistoryStats` message is received and history records exist
- **THEN** the HistoryManager SHALL respond with `HistoryStatsResult` containing `TotalCompleted` (count of Completed records), `TotalFailed` (count of Failed records), `TotalBytes` (sum of all record sizes), `AverageDownloadTimeSeconds` (average of DownloadTimeSeconds over Completed records), `SuccessRate` (TotalCompleted / total records as double)

#### Scenario: Stats with no records
- **WHEN** a `QueryHistoryStats` message is received and no history records exist
- **THEN** the HistoryManager SHALL respond with all values as 0

### Requirement: DownloadHistoryManager handles QueryHistoryCategories
The DownloadHistoryManager SHALL handle `QueryHistoryCategories` messages by extracting distinct category values from its in-memory state and responding with `HistoryCategoriesResult`.

#### Scenario: Categories from records
- **WHEN** a `QueryHistoryCategories` message is received
- **THEN** the HistoryManager SHALL respond with `HistoryCategoriesResult` containing unique category strings sorted alphabetically (case-insensitive deduplication)

#### Scenario: No records
- **WHEN** a `QueryHistoryCategories` message is received and no history records exist
- **THEN** the HistoryManager SHALL respond with an empty array
