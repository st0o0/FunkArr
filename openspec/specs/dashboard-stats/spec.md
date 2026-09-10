# dashboard-stats Specification

## Purpose
TBD - created by archiving change dashboard-richness. Update Purpose after archive.
## Requirements
### Requirement: History stats endpoint
The system SHALL respond to `GET /api/downloads/history/stats` with aggregate statistics computed from all download history records.

#### Scenario: Stats with history
- **WHEN** `GET /api/downloads/history/stats` is requested and history records exist
- **THEN** the response SHALL be JSON with `totalCompleted` (int), `totalFailed` (int), `totalBytes` (long), `averageDownloadTimeSeconds` (int, averaged over completed downloads only), and `successRate` (double, 0.0-1.0)

#### Scenario: Stats with no history
- **WHEN** `GET /api/downloads/history/stats` is requested and no history records exist
- **THEN** the response SHALL be JSON with all counts as 0, `averageDownloadTimeSeconds` as 0, and `successRate` as 0.0

#### Scenario: Actor timeout
- **WHEN** the DownloadHistoryManager does not respond within 10 seconds
- **THEN** the response SHALL be HTTP 504 Gateway Timeout

### Requirement: Dashboard stats row
The Dashboard page SHALL display a stats row with aggregate download history data, fetched on mount via `GET /api/downloads/history/stats`.

#### Scenario: Stats displayed
- **WHEN** the Dashboard loads and history stats are available
- **THEN** the stats row SHALL show total completed count, total failed count, success rate as percentage, total size downloaded (formatted), and average download time (formatted)

#### Scenario: Stats loading
- **WHEN** the Dashboard is loading stats
- **THEN** skeleton placeholders SHALL be shown in the stats row

#### Scenario: Stats fetch failure
- **WHEN** the stats endpoint returns an error
- **THEN** the stats row SHALL be hidden (no error toast - it's supplementary data)

### Requirement: Dashboard queue split cards
The Dashboard stat cards SHALL show the active/queued split from the SSE stream instead of a single "Downloading" count.

#### Scenario: Queue with active and queued items
- **WHEN** the SSE stream reports 2 active and 5 queued items with 3 total slots
- **THEN** the Dashboard SHALL display "Active: 2 / 3" and "Queued: 5" as separate stat cards

#### Scenario: Empty queue
- **WHEN** the SSE stream reports 0 active and 0 queued items
- **THEN** the stat cards SHALL display "Active: 0 / 0" and "Queued: 0"

