## 1. SubtitlePreparer

- [x] 1.1 Create `ISubtitlePreparer` interface with `PrepareAsync(string url, string outputDirectory, CancellationToken ct)` returning `Task<string?>`
- [x] 1.2 Implement TTML/EBU-TT-D to SRT converter — parse `<p begin="" end="">` elements, handle namespaced (`tt:p`) and non-namespaced variants, normalize timestamps to SRT format, decode XML entities, handle `<br/>` and nested `<span>` elements
- [x] 1.3 Implement `SubtitlePreparer` — HTTP download, content sniffing (TTML/WebVTT/SRT/empty), delegate to converter, write temp file, return path or null on any failure
- [x] 1.4 Register `SubtitlePreparer` in DI with `IHttpClientFactory`
- [x] 1.5 Unit tests for TTML→SRT conversion: ARD EBU-TT-D-Basic-DE, ORF plain TTML, nested spans, `<br/>` line breaks, timestamp formats (`HH:MM:SS.mmm` and seconds-only), XML entities
- [x] 1.6 Unit tests for content sniffing: TTML detection, WebVTT detection, SRT detection, empty content, unrecognized format

## 2. Remuxer

- [x] 2.1 Create `IRemuxer` interface matching current `IFfmpegRunner` method signature: `RunAsync(string videoUrl, string? subtitleUrl, string outputPath, Action<ProgressUpdate> onProgress, CancellationToken ct)`
- [x] 2.2 Implement `Remuxer` — compose `ISubtitlePreparer` + `IFfmpegRunner`, pass local subtitle path to runner, cleanup temp file in finally block
- [x] 2.3 Register `Remuxer` as `IRemuxer` in DI

## 3. Simplify FfmpegRunner

- [x] 3.1 Change `IFfmpegRunner.RunAsync` parameter from `subtitleUrl` to `subtitlePath` (local file path)
- [x] 3.2 Update `FfmpegRunner.BuildArguments` to use `FromFileInput` for subtitle path instead of `FromUrlInput`
- [x] 3.3 Remove `ResolveSubtitleUrl` method from `FfmpegRunner`
- [x] 3.4 Remove subtitle retry logic from `FfmpegRunner.RunAsync` (`IsSubtitleInputError` check, retry block)
- [x] 3.5 Update existing FfmpegRunner tests for new parameter semantics

## 4. Update DownloadWorker

- [x] 4.1 Change `DownloadWorker` constructor from `IFfmpegRunner` to `IRemuxer`
- [x] 4.2 Remove `IsSubtitleError` check and subtitle retry logic from `HandleFfmpegResult`
- [x] 4.3 Update `StartFfmpeg` to delegate to `IRemuxer.RunAsync`
- [x] 4.4 Update existing DownloadWorker tests for new dependency

## 5. Cleanup

- [x] 5.1 Remove URL-rewriting hack from Search layer if any remnants exist (`ToWebVttUrl` in `TvSearchWorkerState`/`MovieSearchWorkerState`)
- [x] 5.2 Verify build and format pass
- [x] 5.3 Docker rebuild and E2E test: download with subtitles from ARD channel, verify MKV contains SRT subtitle track
