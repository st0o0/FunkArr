## Why

The download lifecycle has several bugs discovered during a comprehensive code review. A DownloadWorker that crashes during retry-wait permanently leaks its manager slot, making the download stuck and unrecoverable without a full restart. The retry semantics have an off-by-one error (MaxRetries=3 gives only 2 retries), retried movies lose their category in the queue, and the HasSubtitles flag reports based on URL presence rather than actual download outcome. These bugs affect reliability of the core download pipeline.

## What Changes

- Fix slot leak when DownloadWorker recovers in Failed status: HandleStart must detect the re-dispatch scenario and either reset or release the slot
- Fix retry off-by-one: change `Attempt < MaxRetries` to `Attempt <= MaxRetries` so MaxRetries=3 means 3 retries (4 total attempts)
- Fix retry losing category: pass the category through DownloadEnqueued when retrying, sourced from the worker's existing state via QueryWorkerStatus or from the DownloadHistoryManager
- Fix HasSubtitles: base the flag on whether the subtitle track was actually muxed into the output, not on SubtitleUrl presence
- Fix ScheduleEnabled race on recovery: default ScheduleEnabled to false (safe default — no dispatch until scheduler confirms) rather than true
- Fix delete creating Failed history: distinguish Cancelled from Failed in download history recording
- Fix subtitle cancellation classification: catch OperationCanceledException in the remuxer and return a cancellation result rather than letting it propagate as a Permanent failure

## Capabilities

### New Capabilities

- `download-recovery-slot-handling`: DownloadWorker recovery must handle the re-dispatch scenario when recovering in Failed status, either by resetting to Initialized to accept StartDownload or by sending SlotFree to release the slot

### Modified Capabilities

- `download-retry`: Fix off-by-one in ShouldRetry (Attempt < MaxRetries should be <=), and pass category through DownloadEnqueued on manager-level retry
- `download-worker`: Fix HandleStart to handle re-dispatch after recovery from Failed phase; fix HasSubtitles to reflect actual subtitle download outcome; fix subtitle cancellation propagation
- `download-scheduling`: Change ScheduleEnabled default from true to false so recovery doesn't race with the scheduler
- `download-history`: Distinguish Cancelled status from Failed in history recording

## Impact

- **DownloadWorker** (`src/FunkArr.Download/Worker/DownloadWorker.cs`): HandleStart guard, HasSubtitles derivation, subtitle cancellation handling
- **DownloadWorkerState** (`src/FunkArr.Download/Worker/DownloadWorkerState.cs`): HasSubtitles field semantics
- **DownloadManager** (`src/FunkArr.Download/Manager/DownloadManager.cs`): HandleRetry category passthrough
- **DownloadManagerState** (`src/FunkArr.Download/Manager/DownloadManagerState.cs`): ScheduleEnabled default, DownloadEnqueued Apply
- **DownloadHistoryManager** (`src/FunkArr.Download/History/DownloadHistoryManager.cs`): Cancelled vs Failed status
- **Remuxer** (`src/FunkArr.Download/Ffmpeg/Remuxer.cs`): Subtitle cancellation handling
- **Persistence**: DownloadEnqueued event may need an optional Category field (extend-only)
- **Tests**: DownloadWorker recovery tests, retry count tests, category roundtrip tests, subtitle flag tests
