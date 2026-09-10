# history-categories Specification

## Purpose
TBD - created by archiving change dashboard-richness. Update Purpose after archive.
## Requirements
### Requirement: History categories endpoint
The system SHALL respond to `GET /api/downloads/history/categories` with a distinct list of category strings from all download history records.

#### Scenario: Categories with history
- **WHEN** `GET /api/downloads/history/categories` is requested and history records exist
- **THEN** the response SHALL be a JSON array of unique category strings, sorted alphabetically

#### Scenario: Categories with no history
- **WHEN** `GET /api/downloads/history/categories` is requested and no history records exist
- **THEN** the response SHALL be an empty JSON array `[]`

#### Scenario: Actor timeout
- **WHEN** the DownloadHistoryManager does not respond within 10 seconds
- **THEN** the response SHALL be HTTP 504 Gateway Timeout

### Requirement: QueryHistoryCategories message
The DownloadHistoryManager SHALL handle `QueryHistoryCategories` messages by responding with `HistoryCategoriesResult(string[] Categories)` derived from distinct categories in the in-memory state.

#### Scenario: Distinct categories
- **WHEN** a `QueryHistoryCategories` message is received
- **THEN** the HistoryManager SHALL respond with all unique category values from history records, case-insensitive deduplication, sorted alphabetically

### Requirement: QueryHistoryStats message
The DownloadHistoryManager SHALL handle `QueryHistoryStats` messages by responding with `HistoryStatsResult` computed from the in-memory state.

#### Scenario: Stats computation
- **WHEN** a `QueryHistoryStats` message is received
- **THEN** the HistoryManager SHALL respond with totals computed from all records: count by status, sum of sizes, and average download time over completed records only

