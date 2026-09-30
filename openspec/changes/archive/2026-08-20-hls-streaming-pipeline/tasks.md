## 1. Fix subtitle extension bug and update FileService

- [x] 1.1 Update `IFileService.GetTempSubtitlePath` to accept an optional extension parameter, default to `.sub` instead of hardcoded `.srt`
- [x] 1.2 Add `IFileService.GetNormalizedSubtitlePath` that always returns `.srt` (used after normalization)
- [x] 1.3 Update `DownloadService` to pass the actual subtitle URL extension (or `.sub` default) to `GetTempSubtitlePath`
- [x] 1.4 Update existing tests for `FileService` path construction

## 2. Subtitle format sniffing and normalization

- [x] 2.1 Create `SubtitleFormat` enum (Srt, WebVtt, Ttml, Unknown) and `SubtitleFormatDetector` with content-based sniffing (read first 512 bytes)
- [x] 2.2 Extract `NormalizeSubtitleAsync` from `MuxingService` into a standalone `SubtitleNormalizer` service with VTT-to-SRT and TTML-to-SRT converters using format detector instead of extension
- [x] 2.3 Write unit tests for format detection (WEBVTT header, XML/TTML, SRT patterns, unknown fallback)
- [x] 2.4 Write unit tests for VTT-to-SRT and TTML-to-SRT conversion

## 3. HLS download support

- [x] 3.1 Create `HlsDownloadService` that invokes FFmpeg with `-i url.m3u8 -map 0:v -map 0:a -c copy output.mp4`, including process timeout and cancellation support
- [x] 3.2 Implement FFmpeg stderr progress parser: regex for `time=HH:MM:SS.ms` and `speed=`, invoke progress callback with elapsed/total duration
- [x] 3.3 Add `DownloadSourceType` (Direct, Hls) to `DownloadRequest` or create a URL type detector utility (extension-based with `.mp4`/`.m3u8`)
- [x] 3.4 Write unit tests for FFmpeg stderr progress parsing
- [x] 3.5 Write integration-style tests for `HlsDownloadService` (mock Process or test with a local .m3u8 fixture)

## 4. Subtitle acquisition stage

- [x] 4.1 Create `SubtitleAcquisitionService` with three paths: HTTP GET for separate URL, ffprobe+ffmpeg extract for HLS embedded, pass-through for no subtitle
- [x] 4.2 Implement ffprobe wrapper: run `ffprobe -v quiet -print_format json -show_streams` on .m3u8, parse JSON for `codec_type=subtitle` streams
- [x] 4.3 Implement ffmpeg subtitle extraction: `ffmpeg -i url.m3u8 -map 0:s:0 -c:s srt output.srt`
- [x] 4.4 Write tests for subtitle acquisition (separate URL, HLS embedded, no subtitle, failure fallback)

## 5. Refactor pipeline to GraphDSL

- [x] 5.1 Define intermediate record types: `VideoDownloadResult` (videoPath, subtitleUrl, sourceType, hlsManifestUrl), `SubtitleResult` (videoPath, subtitlePath), `NormalizedResult` (videoPath, normalizedSubtitlePath)
- [x] 5.2 Create static stage factories: `DownloadStages.Mp4Download()`, `DownloadStages.HlsDownload()`, `SubtitleStages.Acquire()`, `SubtitleStages.Normalize()`, `MuxStages.Remux()`
- [x] 5.3 Refactor `DownloadQueueActor.MaterializeStream()` to use `GraphDSL.Create` with `Partition<DownloadRequest>(2)` routing by URL type, `Merge` after download, then sequential subtitle/normalize/remux stages
- [x] 5.4 Remove subtitle download code from `DownloadService`
- [x] 5.5 Remove subtitle normalization code from `MuxingService`
- [x] 5.6 Wire progress reporting (Self.Tell) into both MP4 and HLS download stages
- [x] 5.7 Verify existing download event persistence (DownloadStarted, DownloadCompleted, DownloadFailed, MuxingStarted, MuxingCompleted, MuxingFailed) still fires correctly from the new graph

## 6. HLS quality probing

- [x] 6.1 Add HLS manifest parsing to `QualityProbeService`: HTTP GET the .m3u8, parse `EXT-X-STREAM-INF` lines for `BANDWIDTH` and `RESOLUTION` attributes
- [x] 6.2 Add `ProbeSource.HlsManifest` to the QualityInfo probe source enum
- [x] 6.3 Update probing orchestration to route .m3u8 URLs to manifest parsing (Phase M) instead of Phase 2/3
- [x] 6.4 Write tests for HLS manifest parsing (single variant, multiple variants, missing resolution, fetch failure)

## 7. Stream supervision updates

- [x] 7.1 Update `StreamSupervision.LoggingDecider` to resume on `Win32Exception` (FFmpeg process errors) and `HttpRequestException` in subtitle stage
- [x] 7.2 Verify supervision behavior with the new multi-stage graph (one stage failure does not stop others)

## 8. Integration testing

- [x] 8.1 Write an end-to-end test for the MP4 download path through the refactored pipeline (download -> subtitle -> normalize -> remux)
- [x] 8.2 Write an end-to-end test for the HLS download path through the pipeline
- [x] 8.3 Test mixed MP4 and HLS downloads running concurrently in the same pipeline
- [x] 8.4 Test pipeline recovery after stream failure (kill-switch, re-materialization)
