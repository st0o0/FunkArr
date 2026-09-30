## MODIFIED Requirements

### Requirement: Subtitle acquisition via HTTP download
The `SubtitleDownloadActor` SHALL be a transient child `ReceiveActor` of `DownloadActor` in namespace `FunkArr.DownloadClient.Pipeline`. It SHALL handle subtitle download when a separate subtitle URL is available.

#### Scenario: Subtitle from separate URL
- **WHEN** the actor receives an `AcquireSubtitle` command with a non-null `SubtitleUrl`
- **THEN** it downloads the subtitle via `IHttpClientFactory`, saves it via `IFileService.SaveSubtitleAsync(nzoId, content, extension)`, tells the parent `SubtitleAcquired(nzoId, true)`, and stops itself

#### Scenario: Subtitle download fails with non-success status
- **WHEN** the HTTP GET returns a non-success status code
- **THEN** the actor logs a warning and tells the parent `SubtitleAcquired(nzoId, false)`, and the pipeline continues without subtitles

#### Scenario: Subtitle download throws exception
- **WHEN** the HTTP download throws any exception (including `OperationCanceledException`)
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
- **WHEN** `ffprobe` or `ffmpeg` fails during extraction (any exception including `OperationCanceledException`)
- **THEN** the actor logs a warning and tells the parent `SubtitleAcquired(nzoId, false)`
