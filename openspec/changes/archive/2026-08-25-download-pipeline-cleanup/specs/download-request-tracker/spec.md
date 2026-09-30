## MODIFIED Requirements

### Requirement: Status query for SABnzbd queue
Each tracker entity SHALL respond to `QueryStatus` with its current nzoId, title, status, category, enqueuedAt timestamp, percentage (int), mb (double), and mbleft (double).

#### Scenario: Active download status query with progress
- **WHEN** `QueryStatus` is received for an entity in "Downloading" state with 50% progress at 100MB/200MB
- **THEN** the tracker SHALL reply with `DownloadStatus(nzoId, title, "Downloading", category, enqueuedAt, percentage: 50, mb: 200.0, mbleft: 100.0)`

#### Scenario: Queued download status query
- **WHEN** `QueryStatus` is received for an entity in "Queued" state
- **THEN** the tracker SHALL reply with `DownloadStatus(nzoId, title, "Queued", category, enqueuedAt, percentage: 0, mb: 0.0, mbleft: 0.0)`

### Requirement: Status updates from DownloadActor
The tracker SHALL accept `ReportProgress(nzoId, status, percentage, mb, mbleft)`, `CompleteDownload(nzoId, outputPath)`, and `FailDownload(nzoId, error)` messages. `ReportProgress` SHALL update both the persisted status (if changed) and the in-memory progress fields (percentage, mb, mbleft). Progress fields SHALL NOT be persisted.

#### Scenario: Status changed with progress
- **WHEN** `ReportProgress("abc123", "Downloading", 50, 100.0, 50.0)` is received
- **THEN** the tracker SHALL persist `RequestStatusChanged` (if status differs from current) and update in-memory progress fields

#### Scenario: Progress update without status change
- **WHEN** `ReportProgress("abc123", "Downloading", 75, 150.0, 50.0)` is received and status is already "Downloading"
- **THEN** the tracker SHALL only update in-memory progress fields without persisting a new event

#### Scenario: Mark completed
- **WHEN** `CompleteDownload("abc123", "/output/file.mkv")` is received
- **THEN** the tracker SHALL persist `RequestCompleted` journal DTO with the output path and timestamp

## REMOVED Requirements

### Requirement: History entry query for SABnzbd history
**Reason**: History query responsibility moved to HistoryActor. DownloadRequestActor no longer serves post-completion queries.
**Migration**: SABnzbd controller queries HistoryActor.GetHistory instead of fan-out to DownloadRequestActor.QueryHistory.

### Requirement: Retry info query
**Reason**: Retry info responsibility moved to HistoryActor. DownloadRequestActor no longer stores or serves retry parameters.
**Migration**: SABnzbd controller queries HistoryActor.GetRetryInfo instead of DownloadRequestActor.QueryRetryInfo.
