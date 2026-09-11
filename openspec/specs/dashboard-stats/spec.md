# dashboard-stats Specification

## Purpose

API endpoint for aggregate download history statistics and Overview page widgets for health status, download progress, recent activity feed, and storage indicators.
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
The Overview page (formerly Dashboard) SHALL NOT display a stats row with aggregate download history data. The history stats cards (completed count, failed count, success rate, total size, average time) SHALL be removed.

#### Scenario: No history stats on Overview
- **WHEN** the Overview page loads
- **THEN** no history stats row SHALL be rendered
- **AND** the page SHALL NOT call `GET /api/downloads/history/stats`

### Requirement: Dashboard queue split cards
The Overview page SHALL display active/queued counts and total speed as a single compact status line instead of separate stat cards.

#### Scenario: Queue status line with activity
- **WHEN** the SSE stream reports 2 active and 5 queued items at 14.2 MB/s with 3 total slots
- **THEN** the Overview SHALL display a single line: "2 downloading - 7 queued - 14.2 MB/s" with an overall progress bar

#### Scenario: Queue status line idle
- **WHEN** the SSE stream reports 0 active and 0 queued items
- **THEN** the status line SHALL display "No active downloads"

### Requirement: Overview recent activity feed
The Overview page SHALL display a recent activity feed showing the last 10 completed or failed downloads. The feed SHALL be fetched on mount via `GET /api/downloads/history?start=0&limit=10`. Each entry SHALL show a status indicator (green check for completed, red cross for failed), the download title, and a relative timestamp.

#### Scenario: Feed with recent downloads
- **WHEN** history contains completed and failed downloads
- **THEN** the feed SHALL list up to 10 entries with status icon, title, and relative time (e.g., "2 hours ago")

#### Scenario: Feed with failure details
- **WHEN** a feed entry has status "Failed"
- **THEN** it SHALL display with a red cross icon and the fail message as a tooltip

#### Scenario: Empty feed
- **WHEN** no download history exists
- **THEN** the feed section SHALL display "No recent activity"

#### Scenario: Feed refresh on visibility
- **WHEN** the browser tab regains focus (visibilitychange event)
- **THEN** the feed SHALL refetch to show updated data

### Requirement: Overview health status line
The Overview page SHALL display a compact health status line summarizing system health. The line SHALL show a single status indicator (green dot for all healthy, amber dot for warnings, red dot for failures) with a short text summary. It SHALL link to the Setup page.

#### Scenario: All checks healthy
- **WHEN** all health checks return status "ok"
- **THEN** the status line SHALL show a green dot with text "System healthy"

#### Scenario: Warnings present
- **WHEN** one or more health checks return "warn" but none return "fail"
- **THEN** the status line SHALL show an amber dot with text describing the warning count (e.g., "1 warning")

#### Scenario: Failures present
- **WHEN** one or more health checks return "fail"
- **THEN** the status line SHALL show a red dot with the failure count and link to Setup

### Requirement: Overview storage indicator
The Overview page SHALL display a compact storage indicator showing used/total space for the complete directory as a thin progress bar with text. The bar SHALL use `status-warn` color when usage exceeds 90%.

#### Scenario: Normal storage usage
- **WHEN** storage is at 60% capacity (30 GB used of 50 GB)
- **THEN** the indicator SHALL show a progress bar at 60% with "30 GB / 50 GB" text

#### Scenario: High storage usage
- **WHEN** storage exceeds 90% capacity
- **THEN** the progress bar SHALL use `status-warn` color instead of `accent`

#### Scenario: Storage fetch failure
- **WHEN** the storage endpoint returns an error
- **THEN** the storage indicator SHALL be hidden

