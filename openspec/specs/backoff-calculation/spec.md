## ADDED Requirements

### Requirement: Backoff delays use Servus BackoffPolicy

All actor retry delay calculations SHALL use `Servus.Resilience.BackoffPolicy` instead of hand-rolled implementations. Delays SHALL use `DelayWithJitter` to apply ±25% random spread.

#### Scenario: Search worker retries with jitter
- **WHEN** a search worker receives a queue-full response and retries
- **THEN** the retry delay is calculated via `BackoffPolicy.DelayWithJitter(attempt)` with base 500ms and max 1500ms

#### Scenario: Download worker retries with jitter
- **WHEN** a download worker schedules a transient-failure retry
- **THEN** the retry delay is calculated via `BackoffPolicy.DelayWithJitter(attempt)` with base from `DownloadOptions.RetryBackoffBase` and max 5 minutes

#### Scenario: Download worker policy uses constructor-time config
- **WHEN** a download worker is created
- **THEN** the `BackoffPolicy` is initialized from the current `DownloadOptions` values and remains fixed for the worker's lifetime
