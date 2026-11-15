## MODIFIED Requirements

### Requirement: DownloadWorker retries transient failures when enabled
The DownloadWorker SHALL retry downloads that fail with a transient failure when retry is enabled in DownloadOptions. The Worker SHALL schedule a self-message via IWithTimers after an exponential backoff delay.

#### Scenario: Transient failure with retry enabled
- **WHEN** FFmpeg fails with a transient error (HTTP 503, timeout, connection reset)
- **AND** RetryEnabled is true
- **AND** the current attempt count is less than or equal to MaxRetries
- **THEN** the Worker SHALL persist a `DownloadFaulted` event with `FailureKind.Transient`
- **AND** schedule a `RetryAttempt` self-message via IWithTimers after the backoff delay
- **AND** NOT send `SlotFree` to the Manager (the slot remains occupied during retry)

#### Scenario: Transient failure with retry disabled
- **WHEN** FFmpeg fails with a transient error
- **AND** RetryEnabled is false
- **THEN** the Worker SHALL persist a `DownloadFaulted` event with `FailureKind.Transient`
- **AND** send `SlotFree` to the Manager
- **AND** send `RecordDownload` to the HistoryManager

#### Scenario: Permanent failure regardless of retry config
- **WHEN** FFmpeg fails with a permanent error (HTTP 404, empty URL, stream gone)
- **THEN** the Worker SHALL persist a `DownloadFaulted` event with `FailureKind.Permanent`
- **AND** send `SlotFree` to the Manager
- **AND** send `RecordDownload` to the HistoryManager
- **AND** NOT schedule a retry

#### Scenario: Max retries exhausted
- **WHEN** FFmpeg fails with a transient error
- **AND** the current attempt count is greater than MaxRetries
- **THEN** the Worker SHALL persist a `DownloadFaulted` event
- **AND** send `SlotFree` to the Manager
- **AND** send `RecordDownload` to the HistoryManager

### Requirement: Manual retry preserves download category
`DownloadManager` SHALL include the download's media category when persisting `DownloadEnqueued` during a manual retry, so that the queue entry retains the correct category (Show or Movie).

#### Scenario: Retry a failed movie download
- **WHEN** `RetryDownload` is received for a movie download that previously failed
- **THEN** `DownloadEnqueued` SHALL be persisted with the original category (Movie)
- **AND** the queue entry SHALL show category Movie, not the default Show

#### Scenario: Retry a failed show download
- **WHEN** `RetryDownload` is received for a show download that previously failed
- **THEN** `DownloadEnqueued` SHALL be persisted with category Show

#### Scenario: Legacy events without category
- **WHEN** a `DownloadEnqueued` event is replayed that has no category field (pre-fix events)
- **THEN** the category SHALL default to Show (backward compatible)
