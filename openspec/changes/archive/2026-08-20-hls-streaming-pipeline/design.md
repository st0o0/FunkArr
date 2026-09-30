## Context

The download pipeline in `DownloadQueueActor` uses Akka Streams but delegates actual work to `DownloadService` (manual HTTP byte-loop) and `MuxingService` (Process.Start ffmpeg). The pipeline is a flat `SelectAsyncUnordered` chain that only handles direct MP4 downloads. ORF and SRF deliver video as HLS streams (.m3u8), which the current HTTP byte-loop cannot process. Subtitles are downloaded inside `DownloadService` and normalized inside `MuxingService`, mixing concerns across stages.

There is also a bug: `FileService.GetTempSubtitlePath` always returns `.srt` extension, so the format-detection code in `MuxingService.NormalizeSubtitleAsync` (which checks file extension) never triggers VTT or TTML conversion.

## Goals / Non-Goals

**Goals:**
- Support HLS (.m3u8) video downloads for ORF/SRF content using FFmpeg
- Refactor the download pipeline into a `GraphDSL`-based Akka Streams graph with explicit stages for each concern
- Separate subtitle acquisition, format normalization, and remuxing into distinct stream stages
- Fix subtitle format detection to use content sniffing instead of file extension
- Add HLS-aware quality probing via master manifest parsing
- Add FFmpeg stderr-based progress tracking for HLS downloads

**Non-Goals:**
- DASH stream support
- yt-dlp integration
- Re-encoding (all paths remain stream-copy)
- Geo-blocking detection or VPN integration
- Changes to the search pipeline (MediathekViewWeb already indexes ORF/SRF)
- Changes to the Newznab/SABnzbd API surface
- Changes to persistence schema or download events

## Decisions

### 1. GraphDSL with Partition/Merge for URL type routing

The pipeline uses `GraphDSL.Create` with a `Partition<DownloadRequest>(2)` at the front to route .mp4 URLs to the existing HTTP download flow and .m3u8 URLs to a new FFmpeg HLS download flow. Both branches produce a common `VideoDownloadResult` record that merges back into a single path for subtitle handling, normalization, and remuxing.

**Why over separate actors:** Keeps everything in one materialized stream graph. Backpressure, concurrency limits, and kill-switch apply uniformly. No message-passing overhead between actors for what is fundamentally a data pipeline.

**Why over a strategy pattern in DownloadService:** The Akka Streams graph makes the branching visible in the topology. Each branch can have different parallelism, timeout, and supervision characteristics. The graph is self-documenting.

### 2. FFmpeg as HLS downloader

For .m3u8 URLs, use FFmpeg with `ffmpeg -i "url.m3u8" -map 0:v -map 0:a -c copy output.mp4`. FFmpeg handles HLS segment fetching, reassembly, and timing alignment. The output is a regular MP4 file that enters the same remux stage as direct downloads.

**Why not download segments manually:** HLS has adaptive bitrate switching, encryption, byte-range segments, and timing alignment complexity. FFmpeg handles all of this. Reimplementing it would be error-prone and maintenance-heavy.

**Why output to MP4 first, not directly to MKV:** Separating download from remux keeps the stages composable. The remux stage adds subtitles and language metadata uniformly regardless of source. A single FFmpeg call that does HLS download + subtitle merge + MKV output would be a monolithic operation that is harder to test, monitor, and recover from.

### 3. Content-based subtitle format sniffing

Replace file-extension-based format detection with content sniffing. Read the first bytes of the subtitle file:
- Starts with `WEBVTT` -> WebVTT format
- Starts with `<?xml` or contains `<tt` in first 512 bytes -> TTML/EBU-TT
- Contains numbered cues with `-->` -> SRT
- Otherwise -> treat as SRT (best effort)

**Why:** Fixes the current bug where `GetTempSubtitlePath` always returns `.srt`, making format detection dead code. Content sniffing is more reliable than extension-based detection since Mediathek APIs don't consistently use correct extensions.

### 4. Subtitle stage with three acquisition paths

The subtitle stage is a single `SelectAsyncUnordered` that handles three cases:
1. `SubtitleUrl` present in the download request: HTTP GET the URL (same as today, but as its own stage)
2. No `SubtitleUrl` but source was HLS: run `ffprobe -v quiet -print_format json -show_streams` on the original .m3u8 to check for embedded subtitle renditions. If found, extract with `ffmpeg -i url.m3u8 -map 0:s:0 -c:s srt output.srt`
3. No subtitle available: pass through with null subtitle path

**Why ffprobe before ffmpeg extract:** Avoids a failing ffmpeg call when no subtitle track exists. ffprobe is fast (no download, just manifest parsing).

### 5. HLS quality probing via manifest parsing

For .m3u8 URLs, the quality probe skips HTTP HEAD and container probing. Instead, it fetches the master manifest via HTTP GET and parses `EXT-X-STREAM-INF` lines for `BANDWIDTH` and `RESOLUTION` attributes. This is faster and more accurate than HEAD probing since HLS manifests explicitly declare available quality tiers.

### 6. Progress tracking for HLS downloads

HLS downloads via FFmpeg don't have a Content-Length. Instead, parse FFmpeg's stderr output for `time=HH:MM:SS.ms` and `speed=X.Xx` values. If the total duration is known from the search result metadata, compute percentage from elapsed time. Otherwise report elapsed time only.

The progress callback signature stays the same (`Action<long, long>`) but the semantics shift: for HLS, the first parameter is elapsed duration in seconds and the second is total duration (or 0 if unknown). The `DownloadQueueActor` already treats 0 total as "unknown size", so this is backwards-compatible.

### 7. Pipeline stage types

Each stage is a static method returning `Flow<TIn, TOut, NotUsed>` or equivalent, making them independently testable:
- `DownloadStages.Mp4Download()` - HTTP byte-loop flow
- `DownloadStages.HlsDownload()` - FFmpeg HLS flow
- `SubtitleStages.Acquire()` - subtitle acquisition flow
- `SubtitleStages.Normalize()` - format conversion flow
- `MuxStages.Remux()` - FFmpeg remux flow

These replace the current `DownloadService` and `MuxingService` method calls inside stream lambdas with proper composable flows.

## Risks / Trade-offs

[FFmpeg HLS download may be slower than direct segment download] -> Acceptable because FFmpeg handles all HLS edge cases (encryption, adaptive bitrate, byte-range segments). Performance is limited by the source CDN, not the download mechanism.

[ffprobe call adds latency to subtitle detection for HLS] -> ffprobe on an HLS manifest is a single HTTP GET + parse, typically <500ms. Only runs when no `url_subtitle` is provided.

[ORF/SRF content may be geo-blocked] -> Out of scope. Users behind geo-blocks need their own VPN/proxy. FunkArr passes through whatever FFmpeg gets. If FFmpeg fails (403/geo-block), the typed `DownloadOutcome.Failure` handles it like any other download failure.

[Breaking the pipeline into more stages increases stream graph complexity] -> Each stage is independently testable and the graph topology is explicit in `GraphDSL`. The current flat chain hides complexity inside service classes.

[FFmpeg stderr parsing for progress is fragile across FFmpeg versions] -> Use a regex pattern that matches the well-established `time=` format. Log unparseable lines at Debug level. Fall back to no progress reporting if parsing fails.

## Open Questions

- Should HLS downloads have a longer timeout than MP4 downloads? HLS segment fetching is inherently slower. Current MP4 timeout is not configurable per-job.
- Should the pipeline support mixed quality tiers from HLS manifests (e.g. offering 720p and 1080p as separate search results)? Currently quality probing only informs result ranking, not download selection.
