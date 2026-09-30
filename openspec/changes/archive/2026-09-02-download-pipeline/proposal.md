## Why

The SABnzbd download API is fully stubbed — it accepts NZB files but discards them, returns empty queues and histories, and never downloads anything. FunkArr cannot function as a usable *arr download client until the Download domain actually fetches video and subtitle streams, remuxes them to MKV via FFmpeg, and reports real progress back through the SABnzbd-compatible API.

## What Changes

- Add Download domain actors: `DownloadManager` (singleton, queue/history state, concurrency control) and `DownloadWorker` (sharded entity, runs FFmpeg, reports progress)
- Introduce `DownloadStatus` enum: `Queued`, `Processing`, `Completed`, `Failed`
- Add download messages: commands (AddDownload, StartDownload), events (DownloadAdded, DownloadProgress, DownloadCompleted, DownloadFailed), queries (QueryQueue, QueryHistory)
- FFmpeg integration: spawn external process with `-progress pipe:1` for machine-readable progress output, supporting both direct HTTP (.mp4) and HLS (.m3u8) sources — covering all MVW senders (ARD, ZDF, ORF, SRF, ARTE, etc.)
- Subtitle embedding: when MVW provides `url_subtitle`, include it as a soft-sub track (SRT) in the MKV output via FFmpeg
- Full progress tracking: parse FFmpeg progress output to derive percentage, speed, ETA, bytes downloaded — exposed through SABnzbd queue slots
- Extend NZB meta fields with `X-FunkArr-*` custom types (Url, SubtitleUrl, Channel, Duration, Size) so the download worker has all info without re-querying MVW
- Wire SABnzbd adapter endpoints to real DownloadManager communication instead of stubs
- Event-sourced persistence (T1) for both Manager and Worker — queue and download state survive restarts

## Capabilities

### New Capabilities
- `download-messages`: Commands, events, queries, and status enum for the download domain
- `download-manager`: Cluster Singleton actor managing download queue, history, concurrency limits, and persistence
- `download-worker`: Sharded Entity actor that runs FFmpeg, parses progress, manages per-download lifecycle
- `ffmpeg-process`: FFmpeg process spawning, argument building (direct/HLS, with/without subtitle), and machine-readable progress parsing

### Modified Capabilities
- `nzb-object-model`: Add `X-FunkArr-*` custom meta fields (Url, SubtitleUrl, Channel, Duration, Size) to NZB generation and parsing
- `sabnzbd-download-api`: Replace stub responses with real DownloadManager communication for queue, history, addfile, delete, and retry endpoints

## Impact

- **New project code**: `FunkArr.Download/` (DownloadManager, DownloadWorker, FFmpeg integration)
- **New messages**: `FunkArr.Messages/Download/` (all download messages + status enum)
- **Modified**: `FunkArr.ArrApi/` (NZB generation adds X-FunkArr metas, SABnzbd endpoints talk to DownloadManager)
- **Modified**: `FunkArr.ArrApi/Newznab/SearchHandler.cs` (NZB generation must include subtitle URL and other metadata from search results)
- **External dependency**: FFmpeg must be available in the container (already in Dockerfile for future use)
- **Persistence**: New event-sourced journals for DownloadManager and DownloadWorker (SQLite, T1)
- **Not in scope**: File naming conventions, sender-specific subtitle APIs, download UI
