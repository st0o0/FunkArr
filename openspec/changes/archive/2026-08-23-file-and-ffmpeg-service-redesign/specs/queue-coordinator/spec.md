## MODIFIED Requirements

### Requirement: Queue ordering and MaxConcurrent enforcement
`QueueCoordinator` SHALL maintain a `_queue` (ordered list of pending jobs), `_active` (set of currently running nzoIds), and `_maxConcurrent` (from config). `TryStartNext()` SHALL start queued jobs when `_active.Count < _maxConcurrent`. `TryStartNext()` SHALL build `StartDownload` messages without `_tempPath` or `_downloadPath` — those are `IFileService` concerns, not scheduling concerns.

#### Scenario: Job started when slot available
- **WHEN** a job is enqueued and `_active.Count < _maxConcurrent`
- **THEN** `QueueCoordinator` SHALL immediately start the job by telling the DownloadCoordinator shard with `StartDownload(nzoId, downloadUrl, subtitleUrl, title)` and persisting `JobStarted`

#### Scenario: QueueCoordinator no longer stores path fields
- **WHEN** `QueueCoordinator` is constructed with `IOptions<DownloadOptions>`
- **THEN** it SHALL read only `ConcurrentDownloads` from the options. It SHALL NOT store `_tempPath` or `_downloadPath` as instance fields.
