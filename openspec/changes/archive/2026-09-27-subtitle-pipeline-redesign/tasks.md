## 1. Type Model

- [x] 1.1 Create `ISubtitleFormat` interface (`Name`, `CanParse`, `Parse`) in FunkArr.Download
- [x] 1.2 Create `SubtitleCue` record (`Start`, `End`, `Text`) in FunkArr.Download
- [x] 1.3 Create `SubtitleTrack` record (`Format` string, `Language`, `Cues`) in FunkArr.Download
- [x] 1.4 Create `SubtitleFailureReason` enum and `SubtitleResult` abstract record with `Succeeded`/`Failed`/`Unavailable` cases in FunkArr.Download
- [x] 1.5 Create `SrtEmitter` -- write `List<SubtitleCue>` as numbered SRT entries with `HH:MM:SS,mmm` timestamps

## 2. Format Classes

- [x] 2.1 Create `TtmlFormat` implementing `ISubtitleFormat` -- extract from existing `TtmlToSrtConverter` (CanParse caches XDocument, Parse reuses it, keep offset normalization, timestamp parsing, text extraction)
- [x] 2.2 Create `WebVttFormat` implementing `ISubtitleFormat` -- parse VTT timestamps, extract cue text, strip tags, skip NOTE/STYLE blocks
- [x] 2.3 Create `SrtFormat` implementing `ISubtitleFormat` -- parse SRT entries, handle both `,` and `.` separators, skip blank entries
- [x] 2.4 Delete `TtmlToSrtConverter.cs` (logic moved to `TtmlFormat`)

## 3. Pipeline Rewrite

- [x] 3.1 Update `ISubtitlePreparer` interface: return `Task<SubtitleResult>` instead of `Task<string?>`
- [x] 3.2 Rewrite `SubtitlePreparer` to use format registry loop: download, strip BOM, iterate [TtmlFormat, WebVttFormat, SrtFormat], first CanParse match wins, parse to cues, emit SRT via SrtEmitter, return `SubtitleResult`
- [x] 3.3 Update `Remuxer` to handle `SubtitleResult` (pattern match on Succeeded/Failed/Unavailable, log failures, extract file path and language)

## 4. FFmpeg Language Parameter

- [x] 4.1 Add `string? subtitleLanguage` parameter to `IFfmpegRunner.RunAsync`
- [x] 4.2 Update `FfmpegRunner.BuildArguments` to use language parameter (default `"deu"`) instead of hardcoded value
- [x] 4.3 Update `Remuxer` to pass `track.Language` from `SubtitleResult.Succeeded` to FFmpeg

## 5. Dead Code Cleanup

- [x] 5.1 Remove `DownloadPhase.SubtitleDownload` from enum, renumber remaining values
- [x] 5.2 Update `CalculatePercentage` and `IsTransient` to remove `SubtitleDownload` handling

## 6. Telemetry

- [x] 6.1 Add `funkarr.download.subtitle_total` counter and `funkarr.download.subtitle_duration_seconds` histogram to `Telemetry.cs`
- [x] 6.2 Instrument `SubtitlePreparer` to record metrics on every outcome (status + format tags)
- [x] 6.3 Register meter instruments in `TelemetrySetupContainer` if needed

## 7. Tests

- [x] 7.1 Add `TtmlFormatTests` (CanParse detection, parse cues, offset normalization, namespaced/non-namespaced, XDocument caching -- migrate and expand existing `TtmlToSrtConverterTests`)
- [x] 7.2 Add `WebVttFormatTests` (CanParse detection, basic cues, short timestamps, NOTE blocks, styling tags, STYLE blocks)
- [x] 7.3 Add `SrtFormatTests` (CanParse detection, basic parsing, dot separator, multi-line text, blank entries)
- [x] 7.4 Add `SrtEmitterTests` (standard output, sequential numbering)
- [x] 7.5 Update `SubtitlePreparerTests` (if existing) or add integration tests for the full pipeline with `SubtitleResult` assertions
- [x] 7.6 Update `RemuxerTests` to verify `SubtitleResult` handling (Succeeded path, Failed path, Unavailable path)
- [x] 7.7 Update `FfmpegRunnerTests` to verify language parameter in `BuildArguments`
- [x] 7.8 Run all test projects to verify no regressions
