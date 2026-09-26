# Download Retry

## Purpose

Configurable auto-retry with exponential backoff for transient download failures. Tracks attempts and classifies failures to decide whether retry is appropriate.

## Requirements

### Requirement: DownloadWorker retries transient failures when enabled
The DownloadWorker SHALL retry downloads that fail with a transient failure when retry is enabled in DownloadOptions. The Worker SHALL schedule a self-message via IWithTimers after an exponential backoff delay.

#### Scenario: Transient failure with retry enabled
- **WHEN** FFmpeg fails with a transient error (HTTP 503, timeout, connection reset)
- **AND** RetryEnabled is true
- **AND** the current attempt count is less than MaxRetries
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
- **AND** the current attempt count equals MaxRetries
- **THEN** the Worker SHALL persist a `DownloadFaulted` event
- **AND** send `SlotFree` to the Manager
- **AND** send `RecordDownload` to the HistoryManager

### Requirement: Retry backoff uses exponential formula
The retry backoff delay SHALL be calculated as `min(RetryBackoffBase * 2^(attempt-1), 5 minutes)`.

#### Scenario: First retry
- **WHEN** RetryBackoffBase is 30 seconds and this is attempt 1 failing
- **THEN** the backoff delay SHALL be 30 seconds

#### Scenario: Second retry
- **WHEN** RetryBackoffBase is 30 seconds and this is attempt 2 failing
- **THEN** the backoff delay SHALL be 60 seconds

#### Scenario: Backoff capped at 5 minutes
- **WHEN** the calculated backoff exceeds 5 minutes
- **THEN** the delay SHALL be capped at 5 minutes

### Requirement: RetryAttempt triggers new download attempt
The DownloadWorker SHALL handle `RetryAttempt` self-messages by persisting a `DownloadAttemptStarted` event and restarting the FFmpeg process.

#### Scenario: Retry attempt starts
- **WHEN** a `RetryAttempt` timer fires
- **THEN** the Worker SHALL persist a `DownloadAttemptStarted` event with the new attempt number
- **AND** set phase to Initialized
- **AND** start the FFmpeg process as in a normal StartDownload

### Requirement: Attempt count tracked in state
The DownloadWorkerState SHALL track the current attempt number. The first attempt is 1.

#### Scenario: Initial attempt
- **WHEN** a download is started for the first time
- **THEN** the attempt count SHALL be 1

#### Scenario: After retry
- **WHEN** a retry starts after a transient failure
- **THEN** the attempt count SHALL increment by 1

#### Scenario: After reset
- **WHEN** a download is reset via ResetDownload
- **THEN** the attempt count SHALL reset to 0

### Requirement: FailureKind enum
The system SHALL define a `FailureKind` enum in `FunkArr.Messages.Download` with values `Transient` and `Permanent`.

#### Scenario: Enum values
- **WHEN** the FailureKind enum is inspected
- **THEN** it SHALL contain `Transient` (0) and `Permanent` (1)

### Requirement: Failure classification from FFmpeg errors
The `FfmpegRunner` SHALL classify errors as transient or permanent based on error string patterns.

#### Scenario: HTTP 503 classified as transient
- **WHEN** FFmpeg error output contains "Server returned 503" or "HTTP error 503"
- **THEN** the failure SHALL be classified as `FailureKind.Transient`

#### Scenario: Connection timeout classified as transient
- **WHEN** FFmpeg error output contains "Connection timed out" or "Operation timed out"
- **THEN** the failure SHALL be classified as `FailureKind.Transient`

#### Scenario: HTTP 404 classified as permanent
- **WHEN** FFmpeg error output contains "Server returned 404" or "HTTP error 404"
- **THEN** the failure SHALL be classified as `FailureKind.Permanent`

#### Scenario: Empty URL classified as permanent
- **WHEN** the video URL is empty or null
- **THEN** the failure SHALL be classified as `FailureKind.Permanent`

#### Scenario: Unknown error classified as permanent
- **WHEN** the error does not match any known transient pattern
- **THEN** the failure SHALL default to `FailureKind.Permanent`

#### Scenario: Cancellation is not retried
- **WHEN** FFmpeg is cancelled via CancellationToken
- **THEN** the failure SHALL be classified as `FailureKind.Permanent` (not retriable)
