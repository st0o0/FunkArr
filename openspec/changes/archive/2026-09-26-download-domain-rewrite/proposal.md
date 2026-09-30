## Why

The Download domain's core actors (DownloadWorker and DownloadHistoryManager) have grown organically and accumulated structural debt. The DownloadWorker loses route info on recovery, has no retry logic for transient network failures, no phase granularity for the UI, uses `DateTimeOffset.UtcNow` instead of `TimeProvider`, and duplicates `RecordDownload` construction at three call sites. The DownloadHistoryManager holds an unbounded list in RAM with no trimming, recomputes stats on every query, and uses linear scans for lookups. Since FunkArr IS the download client (not just a proxy to SABnzbd), transient retry at the Worker level is the correct layer -- Sonarr/Radarr only blacklist-and-search-alternative, which is meaningless for single-source Mediathek streams.

## What Changes

- **DownloadWorker rewrite**: Persist route info (RouteName, ProxyUrl) in DownloadInitialized event. Add explicit phase state machine (Initialized, SubtitleDownload, VideoDownload, Remuxing, Moving, Completed, Failed). Track attempts with AttemptStarted persistence event. Add configurable auto-retry with exponential backoff (default disabled). Classify failures as Transient (HTTP 503, timeout, network) vs Permanent (404, empty URL, stream gone). Use TimeProvider everywhere. Centralize RecordDownload construction.
- **DownloadHistoryManager refactor**: Add MaxRecords FIFO trimming. Pre-aggregate stats as running counters. Use Dictionary index for Contains checks. Bound snapshot size via trim.
- **DownloadOptions expansion**: Add RetryEnabled (bool, default false), MaxRetries (int, default 3), RetryBackoffBase (TimeSpan, default 30s exponential), MaxHistoryRecords (int, default 1000).
- **New persistence events**: DownloadPhaseChanged, DownloadAttemptStarted, updated DownloadInitialized (with route fields), updated DownloadFaulted (with FailureKind).
- **Updated messages**: WorkerStatusResult gains Phase and Attempt fields. DownloadPhase enum used by Worker directly (not derived client-side).

## Capabilities

### New Capabilities
- `download-retry`: Configurable auto-retry with exponential backoff for transient download failures, attempt tracking, and failure classification (transient vs permanent)
- `download-worker-phases`: Explicit phase state machine in DownloadWorker replacing implicit status guards, with phase persistence and recovery
- `download-history-trimming`: Bounded history with FIFO trimming, pre-aggregated stats, and indexed lookups

### Modified Capabilities
- `download-worker`: Route persistence, TimeProvider usage, centralized RecordDownload, updated recovery behavior
- `download-history`: Trimming, pre-aggregated stats, Dictionary index
- `download-options`: New retry and history config fields
- `download-messages`: Updated persistence DTOs (DownloadInitialized with route, DownloadFaulted with FailureKind), updated WorkerStatusResult (Phase, Attempt), AttemptStarted and PhaseChanged events
- `download-phase-progress`: Phase derived in Worker instead of client-side, Worker reports phase in WorkerStatusResult

## Impact

- **FunkArr.Download**: DownloadWorker.cs full rewrite, DownloadWorkerState.cs rewrite, DownloadHistoryManager.cs refactor, DownloadHistoryManagerState.cs refactor
- **FunkArr.Messages/Download**: New FailureKind enum, updated WorkerStatusResult, new AttemptStarted/PhaseChanged-related messages
- **FunkArr.Persistence/Events/Download**: New DownloadAttemptStarted, DownloadPhaseChanged events, updated DownloadInitialized (route fields), updated DownloadFaulted (FailureKind field)
- **FunkArr.Core**: DownloadOptions gains retry and history config fields
- **FunkArr.Api**: Queue endpoint response gains phase and attempt fields
- **FunkArr.ArrApi**: SABnzbd mode=retry mapping verification (flag if broken, not in implementation scope)
- **FunkArr.Download.Tests**: Full test rewrite for Worker, updated tests for HistoryManager
- **Persistence compatibility**: New events are additive. Updated events gain new fields with defaults -- existing journals remain readable (v0.x, no migration needed)
