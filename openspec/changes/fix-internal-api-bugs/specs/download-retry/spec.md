## ADDED Requirements

### Requirement: Manual retry removes history only after acceptance
`DownloadManager` SHALL tell the DownloadHistoryManager `RemoveHistoryEntry` for a download only after `DownloadEnqueued` for that retry has been persisted. A rejected retry SHALL NOT touch history.

#### Scenario: Accepted retry
- **WHEN** `RetryDownload` is received for a download that is not queued
- **THEN** `DownloadEnqueued` is persisted
- **AND** `RemoveHistoryEntry` is sent to the DownloadHistoryManager
- **AND** the reply is `RetryDownloadResult(true, null)`

#### Scenario: Rejected retry keeps history
- **WHEN** `RetryDownload` is received for a download that is already queued
- **THEN** the reply is `RetryDownloadResult(false, "Item is already queued")`
- **AND** no `RemoveHistoryEntry` is sent
