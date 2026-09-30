## Why

Subtitle handling grew organically: a naked `string? SubtitleUrl` threaded through 8+ types, a fragile if/else detection cascade that parses XML twice, silent failures that leave users unaware subtitles were lost, VTT passthrough that relies on implicit FFmpeg conversion (which is buggy -- FFmpeg has no TTML decoder at all and its VTT decoder breaks on styled content), partial BOM handling, zero telemetry, and a dead `DownloadPhase.SubtitleDownload` enum value.

Research confirms: only three formats matter (EBU-TT-D from ARD/ZDF/ORF, WebVTT from BR/SRF/ARTE, SRT from some ORF content), and custom parsing is mandatory since FFmpeg cannot handle TTML and unreliably handles VTT. The format cannot be predicted by channel -- BR delivers VTT while the rest of ARD delivers EBU-TT-D. Content-sniffing is the only reliable approach.

## What Changes

- Introduce `ISubtitleFormat` interface following the SubtitleEdit/pysubs2 pattern: each format is a self-contained class with `CanParse(content)` + `Parse(content)` (detection and parsing together, not separate)
- Introduce `SubtitleCue` as the single canonical intermediate representation (matching pysubs2's SSAEvent / SubtitleEdit's Paragraph pattern)
- Replace the monolithic `SubtitlePreparer` with a format-registry pipeline: loop registered formats, first match wins, parse to cues, emit SRT
- Explicit VTT-to-SRT conversion (FFmpeg's VTT decoder breaks on styled content from SRF/ARTE)
- Parse XML only once (TTML's `CanParse` caches the XDocument for `Parse`)
- Proper BOM handling (strip before all processing, not just detection)
- Distinguish "no subtitles available" from "subtitle download/conversion failed" via a result type instead of bare `null`
- Add subtitle telemetry (attempted/succeeded/failed, format distribution, duration)
- Remove dead `DownloadPhase.SubtitleDownload` enum value
- Add `Language` field (default `"deu"`, extensible) flowing through to FFmpeg metadata
- **BREAKING**: `ISubtitlePreparer` interface changes from `Task<string?>` to a result type

## Capabilities

### New Capabilities
- `subtitle-format-model`: `SubtitleCue` IR, `SubtitleTrack` carrier, `SubtitleResult` discriminated union, `ISubtitleFormat` interface
- `subtitle-formats`: Self-contained format classes (TtmlFormat, WebVttFormat, SrtFormat) each with `CanParse` + `Parse`, plus `SrtEmitter` for unified output
- `subtitle-pipeline`: Orchestrated fetch/detect/parse/emit pipeline with format registry loop
- `subtitle-telemetry`: Metrics for subtitle operations (attempted, succeeded, failed, format, duration)

### Modified Capabilities
- `subtitle-preparer`: Replaced by the new pipeline types and orchestration
- `remuxer`: Adapts to new pipeline result type instead of bare `string?`
- `ffmpeg-runner`: Receives language metadata instead of hardcoded `"deu"`
- `download-worker-phases`: `SubtitleDownload` dead code removed

## Impact

- **FunkArr.Download**: SubtitlePreparer rewritten, TtmlToSrtConverter replaced by TtmlFormat, new WebVttFormat/SrtFormat, Remuxer adapted, FfmpegRunner language param
- **FunkArr.Messages**: `SubtitleUrl` remains as `string?` on commands (URL is still just a string at the boundary)
- **FunkArr.Persistence**: No change (SubtitleUrl on DownloadInitialized stays as string)
- **FunkArr.Download.Tests**: Format class tests, updated SubtitlePreparer/Remuxer tests
- **FunkArr.Download/Telemetry.cs**: New subtitle instruments on existing `FunkArr.Download` meter
