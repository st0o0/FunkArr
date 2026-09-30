## 1. Messages & Infrastructure

- [x] 1.1 Add `DownloadStatus` enum and all download messages (`AddDownload`, `DownloadAdded`, `StartDownload`, `DownloadProgress`, `DownloadCompleted`, `DownloadFailed`, `QueryQueue`, `QueueResult`, `QueryHistory`, `HistoryResult`, `DeleteDownload`, `DeleteDownloadResult`, `RetryDownload`, `RetryDownloadResult`, `IDownloadResponse`) in `FunkArr.Messages/Download/`
- [x] 1.2 Add `IDownloadManager` and `IDownloadRegion` actor key marker interfaces in `FunkArr.Core/ActorKeys.cs`
- [x] 1.3 Add persistence DTOs for download events in `FunkArr.Persistence/Events/Download/`

## 2. FFmpeg Integration

- [x] 2.1 Implement `FfmpegArgumentBuilder` — builds FFmpeg command-line arguments for direct HTTP and HLS sources, with/without subtitle, including `-y`, `-progress pipe:1`, `-c copy`/`-c:s srt`, and language metadata
- [x] 2.2 Implement `FfmpegProgressParser` — parses `-progress pipe:1` key=value output blocks (`out_time_us`, `total_size`, `speed`) into structured `FfmpegProgress` record
- [x] 2.3 Implement `FfmpegProcess` — spawns FFmpeg, reads stdout for progress, captures stderr for errors, reports exit code
- [x] 2.4 Unit tests for `FfmpegArgumentBuilder` and `FfmpegProgressParser` in `FunkArr.Download.Tests`

## 3. DownloadWorker

- [x] 3.1 Implement `DownloadWorkerState` record and extension methods (`Apply`, state transitions)
- [x] 3.2 Implement `DownloadWorker` sharded entity — handles `StartDownload`, spawns FFmpeg via `FfmpegProcess`, sends `DownloadProgress` to Manager, sends `DownloadCompleted`/`DownloadFailed` on exit, retries without subtitle on subtitle failure
- [x] 3.3 Register `DownloadWorker` shard region with `IDownloadRegion` marker and `IWithDownloadId` message extractor
- [x] 3.4 Unit tests for `DownloadWorker` in `FunkArr.Download.Tests`

## 4. DownloadManager

- [x] 4.1 Implement `DownloadManagerState` record and extension methods (queue/history management, Apply for each event type)
- [x] 4.2 Implement `DownloadManager` singleton — handles AddDownload (assign ID, persist, dispatch or stash), DownloadProgress (in-memory update), DownloadCompleted/Failed (move to history, persist, unstash next), QueryQueue, QueryHistory, DeleteDownload, RetryDownload
- [x] 4.3 Add persistence: event-sourced journal for queue/history state, recovery rebuilds state, items Processing at crash time re-queued as Queued
- [x] 4.4 Register `DownloadManager` singleton with `IDownloadManager` marker in actor system setup
- [x] 4.5 Unit tests for `DownloadManagerState` and `DownloadManager` in `FunkArr.Download.Tests`

## 5. NZB Extension

- [x] 5.1 Extend NZB generation in `SearchHandler` to include `X-FunkArr-Url`, `X-FunkArr-SubtitleUrl`, `X-FunkArr-Channel`, `X-FunkArr-Duration`, `X-FunkArr-Size` meta fields from `SearchResultItem`
- [x] 5.2 Add `SubtitleUrl` field to `SearchResultItem` message and pass `url_subtitle` through from MVW response → search workers → search result
- [x] 5.3 Update NZB parsing in SABnzbd adapter to extract all `X-FunkArr-*` meta fields
- [x] 5.4 Unit tests for NZB generation and parsing with custom metas in `FunkArr.ArrApi.Tests`

## 6. SABnzbd Adapter Wiring

- [x] 6.1 Wire `addfile` endpoint to parse NZB, build `AddDownload` message, ask DownloadManager, return `nzo_ids` from `DownloadAdded`
- [x] 6.2 Wire `queue` endpoint to ask DownloadManager `QueryQueue`, translate `QueueResult` to SABnzbd JSON with progress mapping (percentage, mbleft, timeleft, speed)
- [x] 6.3 Wire `history` endpoint to ask DownloadManager `QueryHistory`, translate `HistoryResult` to SABnzbd JSON
- [x] 6.4 Wire `queue delete`, `history delete`, and `retry` endpoints to send `DeleteDownload`/`RetryDownload` to DownloadManager and translate responses
- [x] 6.5 Wire `fullstatus` endpoint to include aggregate speed from active downloads
- [x] 6.6 Integration tests for SABnzbd adapter endpoints with mocked DownloadManager in `FunkArr.ArrApi.Tests`
