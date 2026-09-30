## Why

All German public broadcasters deliver subtitles as TTML/EBU-TT-D XML, which FFmpeg cannot read. The current workaround rewrites URLs to find WebVTT alternatives (ARD `/ebutt/`→`/webvtt/`, ZDF `.xml`→`.vtt`), but this fails for ORF and SRF (no WebVTT endpoint) and KiKA (often returns empty responses). Downloads from these channels silently lose subtitles via a retry-without-subtitles fallback. A universal approach is needed that works for all channels regardless of their subtitle delivery format.

## What Changes

- New `ISubtitlePreparer` component that downloads subtitle content from any URL, detects the format by sniffing content, converts TTML/EBU-TT-D to SRT, and writes a local temp file
- New `IRemuxer` component that orchestrates subtitle preparation → FFmpeg execution → temp file cleanup, replacing `IFfmpegRunner` as the `DownloadWorker`'s dependency
- `IFfmpegRunner` simplified: `subtitleUrl` parameter becomes `subtitlePath` (local file path), all subtitle retry/error handling removed
- `DownloadWorker` simplified: depends on `IRemuxer` instead of `IFfmpegRunner`, subtitle retry logic removed
- Remove `ResolveSubtitleUrl` URL-rewriting hack from `FfmpegRunner`

## Capabilities

### New Capabilities
- `subtitle-preparer`: Downloads subtitle content from a URL, detects format (TTML/EBU-TT-D/WebVTT/SRT/empty), converts to SRT when needed, and provides a local file path for FFmpeg consumption
- `remuxer`: Orchestrates the full media remux pipeline — subtitle preparation, FFmpeg execution with local inputs, and temp file cleanup

### Modified Capabilities
- `ffmpeg-runner`: Parameter changes from remote subtitle URL to local subtitle file path; all subtitle error detection and retry logic removed
- `download-worker`: Dependency changes from `IFfmpegRunner` to `IRemuxer`; subtitle retry logic removed from actor

## Impact

- **FunkArr.Download**: New files (`ISubtitlePreparer.cs`, `SubtitlePreparer.cs`, `IRemuxer.cs`, `Remuxer.cs`), modified files (`FfmpegRunner.cs`, `IFfmpegRunner.cs`, `DownloadWorker.cs`, `DownloadServiceExtensions.cs`)
- **DI registration**: New services registered, `IRemuxer` replaces `IFfmpegRunner` in worker injection
- **Tests**: New unit tests for TTML→SRT conversion and subtitle format detection; updated tests for simplified FfmpegRunner and DownloadWorker
- **No API changes**: NZB format, Newznab API, SABnzbd API all unchanged — subtitle URLs flow through unchanged, conversion happens at download time
