## 1. Messages and Persistence DTOs

- [x] 1.1 Add `FailureKind` enum to `FunkArr.Messages.Download` (Transient, Permanent)
- [x] 1.2 Extend `DownloadPhase` enum with full pipeline values (Initialized, SubtitleDownload, VideoDownload, Remuxing, Moving, Completed, Failed)
- [x] 1.3 Add `PersistedFailureKind` enum to `FunkArr.Persistence` and mapping extensions in `FunkArr.Download.PersistenceMapping`
- [x] 1.4 Update `DownloadInitialized` persistence DTO: add `RouteName` (string, default "Direct") and `ProxyUrl` (string?, default null) fields
- [x] 1.5 Update `DownloadFaulted` persistence DTO: add `FailureKind` (PersistedFailureKind, default Permanent) field
- [x] 1.6 Add `DownloadAttemptStarted` persistence DTO (DownloadId, Attempt)
- [x] 1.7 Add `DownloadPhaseChanged` persistence DTO (DownloadId, Phase as int)
- [x] 1.8 Add `HistoryTrimmed` persistence DTO (Count)
- [x] 1.9 Update `WorkerStatusResult` message: add `Phase` (DownloadPhase) and `Attempt` (int) fields
- [x] 1.10 Update `FfmpegResult` record: add `FailureKind` (FailureKind, default Permanent) field

## 2. DownloadOptions

- [x] 2.1 Add `RetryEnabled` (bool, default false), `MaxRetries` (int, default 3), `RetryBackoffBase` (TimeSpan, default 30s), `MaxHistoryRecords` (int, default 1000) to `DownloadOptions`

## 3. FfmpegRunner Failure Classification

- [x] 3.1 Add `ClassifyFailure(string? error)` method to `FfmpegRunner` returning `FailureKind` based on error string patterns (transient: 503, timeout, connection reset; permanent: 404, 403, unknown)
- [x] 3.2 Update `FfmpegRunner.RunAsync` to populate `FailureKind` on `FfmpegResult`
- [x] 3.3 Add unit tests for `ClassifyFailure` covering transient patterns, permanent patterns, null/empty input, and cancellation

## 4. DownloadWorkerState Rewrite

- [x] 4.1 Rewrite `DownloadWorkerState` record: add `RouteName`, `ProxyUrl`, `Phase` (DownloadPhase), `Attempt` (int) fields
- [x] 4.2 Update `Apply` extensions for all existing events (DownloadInitialized with route fields, DownloadStarted, DownloadSucceeded, DownloadFaulted with FailureKind)
- [x] 4.3 Add `Apply` extensions for new events (DownloadAttemptStarted, DownloadPhaseChanged)
- [x] 4.4 Add `ToRecordDownload(TimeProvider, DataPaths.ResolvedDownload?)` extension method replacing 3-site duplication

## 5. DownloadWorker Rewrite

- [x] 5.1 Add `IWithTimers` to DownloadWorker, inject `TimeProvider`, add `IOptionsMonitor<DownloadOptions>` for retry config
- [x] 5.2 Rewrite `HandleInit`: persist DownloadInitialized with RouteName and ProxyUrl from message
- [x] 5.3 Rewrite `HandleStart`: persist `DownloadAttemptStarted`, use route info from state, use `TimeProvider` for timestamps
- [x] 5.4 Rewrite `HandleFfmpegResult`: use `FailureKind` from result, delegate to retry or failure path, use `ToRecordDownload` for history notification
- [x] 5.5 Add retry logic: `RetryAttempt` self-message type, timer scheduling with exponential backoff, attempt guard against MaxRetries
- [x] 5.6 Add phase tracking: update phase in `HandleProgress` based on progress data (infer from bytes vs time), set phase on start/complete/fail
- [x] 5.7 Update `HandleQueryStatus`: include Phase and Attempt in WorkerStatusResult
- [x] 5.8 Update `OnRecoveryCompleted`: reset transient phases (SubtitleDownload, VideoDownload, Remuxing, Moving) to Initialized
- [x] 5.9 Add recovery handlers for new persistence events (DownloadAttemptStarted, DownloadPhaseChanged)
- [x] 5.10 Update `HandleReset`: reset attempt count to 0

## 6. DownloadHistoryManager Refactor

- [x] 6.1 Add `HistoryStats` record to `DownloadHistoryManagerState` (TotalCompleted, TotalFailed, TotalBytes, TotalDownloadTimeSeconds)
- [x] 6.2 Add `HashSet<Guid>` index to state, update `Contains` to use it
- [x] 6.3 Update `Apply(HistoryRecorded)`: maintain running stats and HashSet index
- [x] 6.4 Update `Apply(HistoryRemoved)`: maintain running stats and HashSet index
- [x] 6.5 Add trimming logic: after adding record, trim oldest if count exceeds MaxHistoryRecords, persist `HistoryTrimmed` event
- [x] 6.6 Add `Apply(HistoryTrimmed)` and recovery handler
- [x] 6.7 Update `ToHistoryStats()` to use pre-aggregated counters instead of iterating all records
- [x] 6.8 Update `GetPersistenceState` / `FromPersistence` for new state structure

## 7. DownloadManager Integration

- [x] 7.1 Update `HandleAdd`: pass RouteName and ProxyUrl through InitDownload (already done, verify no changes needed)
- [x] 7.2 Update `HandleQueryQueue`: map new Phase and Attempt fields into QueueItem response

## 8. API Updates

- [x] 8.1 Update internal queue API response model to include `phase` and `attempt` fields
- [x] 8.2 Verify SABnzbd API `mode=retry` maps to ResetDownload + re-queue flow (flag if broken)

## 9. Tests

- [x] 9.1 Write DownloadWorker tests: init with route info persisted, start with attempt tracking, phase transitions
- [x] 9.2 Write DownloadWorker retry tests: transient failure triggers retry when enabled, permanent failure does not retry, max retries exhausted, backoff calculation
- [x] 9.3 Write DownloadWorker tests: recovery resets transient phases, route info survives recovery, TimeProvider used for timestamps
- [x] 9.4 Write DownloadHistoryManager tests: trimming at MaxRecords, pre-aggregated stats accuracy, HashSet index consistency
- [x] 9.5 Update existing DownloadManager tests if QueueItem shape changed
- [x] 9.6 Run all test projects, fix any regressions
