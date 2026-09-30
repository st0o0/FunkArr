# FFmpeg Runner

## MODIFIED Requirements

### Requirement: FfmpegRunner is a static facade
The FfmpegRunner SHALL be a static class that encapsulates FFmpeg process lifecycle, progress parsing, and result reporting behind a single `Run` method.

#### Scenario: Run signature
- **WHEN** a caller invokes `FfmpegRunner.Run`
- **THEN** it SHALL accept `IActorRef self`, `string videoUrl`, `string? subtitlePath`, `string outputPath`
- **AND** `subtitlePath` SHALL be a local file path (not a URL), or null
- **AND** return a `CancellationTokenSource` for cancellation control

## REMOVED Requirements

### Requirement: DownloadWorker subtitle failure is non-fatal
**Reason**: Subtitle error detection and retry-without-subtitles logic moves to the `Remuxer`/`SubtitlePreparer` layer. `FfmpegRunner` no longer receives remote subtitle URLs, so subtitle input errors cannot occur.
**Migration**: `ISubtitlePreparer` handles all subtitle failures before FFmpeg starts. If preparation fails, `IRemuxer` passes null to `FfmpegRunner`.
