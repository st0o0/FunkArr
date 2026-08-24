## Purpose

Subtitle acquisition, content-based format sniffing, and normalization to SRT as dedicated transient child actors of `DownloadActor` and stateless utility classes. Three actors handle the pipeline stages: `SubtitleDownloadActor`, `SubtitleExtractActor`, and `SubtitleConvertActor`. Two static utilities handle format detection and conversion: `SubtitleFormatDetector` and `SubtitleNormalizer`.

## Requirements

### Requirement: Subtitle acquisition via HTTP download
The `SubtitleDownloadActor` SHALL be a transient child `ReceiveActor` of `DownloadActor` in namespace `FunkArr.DownloadClient.Pipeline`. It SHALL handle subtitle download when a separate subtitle URL is available.

#### Scenario: Subtitle from separate URL
- **WHEN** the actor receives an `AcquireSubtitle` command with a non-null `SubtitleUrl`
- **THEN** it downloads the subtitle via `IHttpClientFactory`, saves it via `IFileService.SaveSubtitleAsync(nzoId, content, extension)`, tells the parent `SubtitleAcquired(nzoId, true)`, and stops itself

#### Scenario: Subtitle download fails with non-success status
- **WHEN** the HTTP GET returns a non-success status code
- **THEN** the actor logs a warning and tells the parent `SubtitleAcquired(nzoId, false)`, and the pipeline continues without subtitles

#### Scenario: Subtitle download throws exception
- **WHEN** the HTTP download throws an exception (not `OperationCanceledException`)
- **THEN** the actor logs a warning and tells the parent `SubtitleAcquired(nzoId, false)`

### Requirement: Subtitle extraction from HLS manifest
The `SubtitleExtractActor` SHALL be a transient child `ReceiveActor` of `DownloadActor` in namespace `FunkArr.DownloadClient.Pipeline`. It SHALL extract subtitles from HLS manifests when no separate subtitle URL exists.

#### Scenario: Subtitle from HLS manifest (fallback)
- **WHEN** the actor receives an `AcquireSubtitle` command with a non-null `HlsManifestUrl`
- **THEN** it calls `IFfmpegService.ExtractSubtitleAsync(nzoId, manifestUrl)`, which probes for subtitle streams via `ffprobe` and extracts the first subtitle track via `ffmpeg -i url.m3u8 -map 0:s:0 -c:s srt output.srt`

#### Scenario: No subtitle in HLS manifest
- **WHEN** `ffprobe` finds no subtitle streams in the manifest
- **THEN** the actor tells the parent `SubtitleAcquired(nzoId, false)` and the pipeline continues without subtitles

#### Scenario: Extraction fails gracefully
- **WHEN** `ffprobe` or `ffmpeg` fails during extraction
- **THEN** the actor logs a warning and tells the parent `SubtitleAcquired(nzoId, false)`

### Requirement: Subtitle format conversion
The `SubtitleConvertActor` SHALL be a transient child `ReceiveActor` of `DownloadActor` in namespace `FunkArr.DownloadClient.Pipeline`. It SHALL normalize downloaded subtitles to SRT format via `IFileService.NormalizeSubtitleAsync(nzoId)`.

#### Scenario: Conversion succeeds
- **WHEN** the actor receives a `ConvertSubtitle(nzoId)` command
- **THEN** it calls `IFileService.NormalizeSubtitleAsync(nzoId)`, tells the parent `SubtitleConverted(nzoId)` on success, and stops itself

#### Scenario: Conversion returns null
- **WHEN** `NormalizeSubtitleAsync` returns null
- **THEN** the actor tells the parent `WorkerFailed(nzoId, FailureKind.Malformed, ...)` 

#### Scenario: Conversion throws exception
- **WHEN** normalization throws an exception
- **THEN** the actor tells the parent `WorkerFailed(nzoId, FailureKind.Malformed, ex.Message)` and stops itself

### Requirement: Content-based subtitle format sniffing
The `SubtitleFormatDetector` static class in namespace `FunkArr.Subtitle` SHALL detect subtitle format by inspecting the first 512 bytes of file content. It supports `byte[]`, `string`, and file-based detection via `DetectFromFileAsync`.

#### Scenario: WebVTT detected by content
- **WHEN** the subtitle file content (after BOM/whitespace trimming) starts with `WEBVTT`
- **THEN** the format is detected as `SubtitleFormat.WebVtt`

#### Scenario: TTML detected by content
- **WHEN** the subtitle file content starts with `<?xml` or contains `<tt` within the first 512 bytes
- **THEN** the format is detected as `SubtitleFormat.Ttml`

#### Scenario: SRT detected by content
- **WHEN** the subtitle file content contains `-->` within the first 512 bytes
- **THEN** the format is detected as `SubtitleFormat.Srt`

#### Scenario: Unknown format
- **WHEN** the subtitle file content does not match any known format pattern
- **THEN** the format is detected as `SubtitleFormat.Unknown`

### Requirement: Subtitle normalization to SRT
The `SubtitleNormalizer` static class in namespace `FunkArr.Subtitle` SHALL convert subtitle files to SRT format based on detected format.

#### Scenario: WebVTT normalized to SRT
- **WHEN** the subtitle format is detected as WebVTT
- **THEN** `ConvertVttToSrt` removes WEBVTT header, STYLE/NOTE blocks, converts `.` millisecond separators to `,`, and adds sequential cue numbers

#### Scenario: TTML normalized to SRT
- **WHEN** the subtitle format is detected as TTML
- **THEN** `ConvertTtmlToSrt` extracts `<p>` elements with `begin`/`end` attributes, strips inner HTML tags, normalizes timestamps (`.` to `,`), and produces sequential SRT entries

#### Scenario: SRT passes through unchanged
- **WHEN** the subtitle format is detected as SRT
- **THEN** the file is copied to the output path without conversion

### Requirement: Transient child actor lifecycle
All three subtitle actors SHALL stop themselves after completing their work (success or failure) via `Context.Stop(Self)` in a `finally` block.

#### Scenario: Actor self-terminates after work
- **WHEN** subtitle acquisition, extraction, or conversion completes
- **THEN** the actor stops itself, freeing resources in the parent's child collection
