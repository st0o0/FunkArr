## Why

FunkArr has three independent backoff implementations: hardcoded `TimeSpan[]` arrays in search workers, a manual `Math.Pow` calculation in DownloadWorker, and Akka's built-in `BackoffSupervisor`. Servus 0.35.0 (already referenced) ships `Servus.Resilience.Backoff` — a pure delay calculator with exponential backoff and built-in jitter. Replacing the hand-rolled code with it removes duplication, adds jitter to prevent thundering herd on MediathekViewWeb queue-full retries, and establishes a single backoff pattern across the codebase.

## What Changes

- Replace `TimeSpan[] _retryBackoff` arrays in `TvSearchWorker` and `MovieSearchWorker` with a `static readonly BackoffPolicy` using `DelayWithJitter`
- Replace `CalculateBackoff()` method and `_maxBackoff` field in `DownloadWorker` with a `BackoffPolicy` instance created in the constructor from `DownloadOptions`
- Use `DelayWithJitter` everywhere — no scenario in FunkArr benefits from deterministic retry timing

## Capabilities

### New Capabilities

_(none — this is a refactor of existing retry delay calculation, not a new capability)_

### Modified Capabilities

_(none — retry behavior requirements are unchanged; only the delay calculation implementation changes)_

## Impact

- **Search domain**: `TvSearchWorker.cs`, `MovieSearchWorker.cs` — delay calculation changes from array index to `BackoffPolicy.DelayWithJitter(attempt)`. Retry timing shifts slightly due to jitter (±25% spread around the same base values).
- **Download domain**: `DownloadWorker.cs` — `CalculateBackoff` method removed, replaced by `BackoffPolicy` field. Config-driven base delay preserved via constructor initialization from `IOptionsMonitor<DownloadOptions>`.
- **No API changes**, no message changes, no persistence changes.
- **Not touched**: `AkkaSetupContainer` `BackoffSupervisor` (Akka-native), `RetryAfterDefaults` (Polly HTTP resilience).
