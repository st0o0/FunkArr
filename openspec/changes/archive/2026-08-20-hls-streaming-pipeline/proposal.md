## Why

FunkArr only supports direct MP4 downloads (ARD/ZDF). ORF and SRF deliver video as HLS streams (.m3u8), which the current HTTP byte-loop in DownloadService cannot handle. Adding HLS support unlocks Austrian and Swiss public broadcaster content. The download pipeline should also be refactored into a proper Akka Streams graph where each concern (download, subtitle acquisition, subtitle normalization, remuxing) is an explicit stage rather than buried in monolithic service classes.

## What Changes

- Refactor `DownloadQueueActor` pipeline from flat `SelectAsyncUnordered` chain into a `GraphDSL`-based Akka Streams graph with Partition/Merge for URL type routing
- Add HLS download flow using FFmpeg to fetch and assemble .m3u8 streams into MP4
- Extract subtitle download into its own stream stage with three paths: separate URL (HTTP GET), HLS-embedded (ffprobe + ffmpeg extract), or no subtitle
- Extract subtitle normalization into its own stream stage using content-based format sniffing (not file extension) to fix the existing dead-code bug where VTT/TTML conversion never triggers
- Extract remuxing into its own stream stage
- Add HLS-aware quality probing: parse master .m3u8 manifests for bandwidth/resolution instead of HTTP HEAD
- Add FFmpeg stderr progress parsing for HLS downloads (time=, speed=) since Content-Length is unavailable

## Capabilities

### New Capabilities

- `hls-download`: HLS stream download via FFmpeg, URL type detection (.mp4 vs .m3u8), FFmpeg stderr progress parsing
- `subtitle-processing`: Subtitle acquisition (HTTP GET, HLS extraction via ffprobe/ffmpeg, absent handling), format sniffing (WEBVTT/TTML/SRT detection by content), normalization to SRT

### Modified Capabilities

- `download-pipeline`: Pipeline topology changes from flat SelectAsyncUnordered chain to GraphDSL with Partition/Merge, stages become composable stream stages instead of service calls
- `muxing-pipeline`: Remuxing becomes a dedicated stream stage, subtitle normalization moves out to its own stage
- `download-service`: MP4 download becomes one branch of the partitioned graph instead of the only path
- `quality-probing`: Add HLS manifest parsing path alongside existing HTTP HEAD probing
- `stream-supervision`: Supervision decider must handle new failure modes from FFmpeg HLS downloads and ffprobe calls
- `file-operations`: Fix subtitle temp path to preserve original format extension for proper content-based sniffing

## Impact

- `src/FunkArr/DownloadClient/DownloadQueueActor.cs` - major refactor of stream materialization
- `src/FunkArr/DownloadClient/DownloadService.cs` - split into MP4 download stage
- `src/FunkArr/Muxing/MuxingService.cs` - split into remux stage, subtitle normalization extracted
- `src/FunkArr/Shared/FileService.cs` - fix subtitle extension bug
- `src/FunkArr/Search/QualityProbeService.cs` - add HLS manifest quality parsing
- `src/FunkArr/DownloadClient/DownloadRequest.cs` - URL type discriminator
- `src/FunkArr/DownloadClient/DownloadResult.cs` - intermediate stage result types
- `src/FunkArr/Shared/StreamSupervision.cs` - new failure mode handling
- New files for HLS download logic and subtitle processing stages
- No API surface changes (Newznab/SABnzbd APIs unchanged)
- No persistence schema changes (download events/DTOs unchanged)
- Docker image already includes FFmpeg
