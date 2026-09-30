## Context

The SABnzbd download API (`/download/api`) is fully stubbed. The adapter parses NZB files but discards them, returns empty queues, and never triggers a download. The `FunkArr.Download` project exists but contains no source code. The Search domain already produces `SearchResultItem` with all data needed to drive a download (video URL, subtitle URL, channel, duration, size).

MediathekViewWeb serves content from all DACH public broadcasters. German senders (ARD, ZDF, BR, HR, MDR, NDR, WDR, SWR, RBB, arte, 3sat, KiKA, phoenix, funk) use direct HTTP `.mp4` URLs. Austrian (ORF) and Swiss (SRF) senders use HLS `.m3u8` streams. FFmpeg handles both transparently with `-i <url> -c copy`.

MVW provides a single `url_subtitle` per item (TTML format, from ARD/ORF APIs). When present, FFmpeg embeds it as a soft-sub SRT track in the MKV output.

## Goals / Non-Goals

**Goals:**
- Download video from any MVW-supported sender (direct HTTP and HLS)
- Embed available subtitle as soft-sub track in MKV output
- Full progress tracking (percentage, speed, ETA, bytes) via FFmpeg's `-progress` output
- Event-sourced persistence (T1) for queue and download state
- Real SABnzbd API responses from live DownloadManager state

**Non-Goals:**
- File naming conventions for Sonarr/Radarr import (separate change)
- Sender-specific subtitle APIs for multiple subtitle tracks (separate change)
- Download UI (separate change)
- Re-encoding or quality selection at download time (search already picks best quality)
- Retry logic with exponential backoff (simple retry via SABnzbd retry endpoint is sufficient)

## Decisions

### 1. FFmpeg as universal downloader

**Decision:** Use FFmpeg for all downloads — both `.mp4` and `.m3u8` sources.

**Alternatives considered:**
- HttpClient for `.mp4`, FFmpeg only for `.m3u8`: More code, two download paths, separate remux step needed anyway for subtitle embedding.
- HttpClient + custom HLS segment downloader: Massive complexity, reimplements what FFmpeg does natively.

**Rationale:** One FFmpeg invocation handles video download, subtitle download, format conversion (TTML→SRT), and MKV muxing in a single process. The `-c copy` flag avoids re-encoding — it's a stream copy, fast and lossless. This eliminates the need for separate download and remux steps.

### 2. Actor topology: Manager + Worker, no children

**Decision:** Two actors only — `DownloadManager` (Cluster Singleton) and `DownloadWorker` (Sharded Entity). No child actor hierarchy within the worker.

**Alternatives considered:**
- Worker with child actors per step (VideoDownloadActor, SubtitleDownloadActor, RemuxActor): Over-engineered. FFmpeg does all steps in one process. Child coordination adds complexity without value.
- Manager only, no worker: Manager becomes too complex. Separation keeps queue management (Manager) apart from process execution (Worker).

**Rationale:** The worker's job is: receive command → build FFmpeg args → spawn process → parse progress → report result. That's a single responsibility. The manager's job is: accept downloads → enforce concurrency → track queue/history → answer queries. Clean separation, no internal choreography.

### 3. Progress via `-progress pipe:1`

**Decision:** Use FFmpeg's machine-readable progress output on stdout (`-progress pipe:1`) rather than parsing stderr.

**Alternatives considered:**
- Parse stderr regex (`time=00:01:23.45 size=...`): Fragile, format varies across FFmpeg versions, mixed with other log output.
- No progress, just poll exit code: Loses speed/ETA/percentage data for SABnzbd API.

**Rationale:** `-progress pipe:1` produces clean key=value pairs (`out_time_us`, `total_size`, `speed`) on stdout, one block per progress interval. Reliable, version-stable, trivially parseable. stderr stays available for error diagnostics on failure.

### 4. NZB extended with X-FunkArr-* meta fields

**Decision:** Embed all download-relevant data in the NZB via custom `X-FunkArr-*` meta types. The NZB spec explicitly supports `X-` prefixed custom types.

Fields:
- `X-FunkArr-Url` — video URL (replaces current `url` meta which is also used)
- `X-FunkArr-SubtitleUrl` — subtitle URL (empty string if none)
- `X-FunkArr-Channel` — broadcaster channel name
- `X-FunkArr-Duration` — duration in seconds (for progress percentage calculation)
- `X-FunkArr-Size` — estimated size in bytes

**Alternatives considered:**
- Re-query MVW at download time: Extra latency, MVW rate limits, content may have been removed between search and download.
- Store in database keyed by NZB ID: Adds persistence coupling between adapter and domain.

**Rationale:** NZB is already the transport format between Sonarr/Radarr and FunkArr. Enriching it with all required metadata makes the download self-contained — the worker needs nothing beyond the NZB content.

### 5. DownloadStatus as flat enum

**Decision:** Four states: `Queued`, `Processing`, `Completed`, `Failed`.

```
Queued ──▶ Processing ──▶ Completed
               │
               └──▶ Failed
```

SABnzbd adapter mapping:
- `Queued` → `"Queued"` (queue response)
- `Processing` → `"Downloading"` (queue response)
- `Completed` → `"Completed"` (history response)
- `Failed` → `"Failed"` (history response)

**Rationale:** FFmpeg performs download and remux in a single process — there is no distinct "remuxing" phase. `Processing` honestly represents "FFmpeg is running". The SABnzbd adapter translates to the status strings Sonarr/Radarr expect.

### 6. Concurrency control via stashing in Manager

**Decision:** DownloadManager uses Akka stashing (same pattern as MediathekViewWebManager) to limit concurrent downloads. Configurable limit, default 3.

**Rationale:** Proven pattern already in the codebase. When at capacity, incoming AddDownload messages are stashed. When a worker reports completion/failure, one message is unstashed. Simple, reliable, no custom queue data structure needed.

### 7. Persistence tier T1 (event-sourced)

**Decision:** Both DownloadManager and DownloadWorker use T1 event-sourced persistence.

- **Manager persists:** queue entries (download ID, metadata, status) and history entries. Recovery rebuilds the full queue/history state.
- **Worker persists:** download command and final status. Recovery can restart a failed-in-progress download or skip completed ones.

**Rationale:** Downloads are critical user-facing operations. Queue state must survive restarts — a download accepted but lost on crash would be silently dropped by Sonarr. T1 is appropriate per project conventions.

## Risks / Trade-offs

**[FFmpeg not installed]** → The container must include FFmpeg. Check Dockerfile — if not present, add `ffmpeg` package to the base image. Fail fast with a clear error on startup if `ffmpeg` is not on PATH.

**[HLS geo-restrictions]** → Some ORF/SRF content may be geo-restricted. FFmpeg will fail with HTTP 403. → Report as `Failed` with the FFmpeg error message. No automatic circumvention.

**[Long-running FFmpeg process and actor restarts]** → If the actor system restarts while FFmpeg is running, the process becomes orphaned. → Worker recovery checks for orphaned processes (by PID stored in state) or simply re-starts the download. FFmpeg with `-c copy` is fast enough that re-downloading is acceptable.

**[Progress message volume]** → FFmpeg emits progress roughly every second. For a 90-minute video, that's ~5400 progress messages. → Don't persist progress — only persist state transitions (Queued→Processing→Completed/Failed). Progress is ephemeral, held in Manager's in-memory state only.

**[Subtitle format compatibility]** → MVW returns TTML URLs (ARD) and occasionally VTT. FFmpeg handles both, converting to SRT for MKV embedding. → If a subtitle URL fails to download or convert, proceed without subtitles rather than failing the entire download.
