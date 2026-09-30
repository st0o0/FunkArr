## ADDED Requirements

### Requirement: IDownloadResponse marker interface

FunkArr.Messages SHALL define an `IDownloadResponse` marker interface in namespace `FunkArr.Messages.Download`. All Download domain response types SHALL implement it.

#### Scenario: IDownloadResponse interface

- **WHEN** `IDownloadResponse` is defined
- **THEN** it SHALL be in namespace `FunkArr.Messages.Download` and `DownloadAdded`, `QueueResult`, `HistoryResult`, `HistoryStatsResult`, `HistoryCategoriesResult`, `DeleteDownloadResult`, `RetryDownloadResult`, and `WorkerStatusResult` SHALL implement it

#### Scenario: Download endpoints use typed Ask

- **WHEN** a Download API endpoint sends a query or command to a Download actor
- **THEN** it SHALL use `Ask<IDownloadResponse>` instead of `Ask<object>`
