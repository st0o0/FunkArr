# Subtitle Pipeline

## Purpose

Orchestrates the full subtitle workflow: download content, strip BOM, detect format via registered `ISubtitleFormat` instances, parse to cues, emit as SRT, and return a `SubtitleResult`.

## Requirements

### Requirement: SubtitlePipeline orchestrates fetch-detect-parse-emit
The `SubtitlePreparer` SHALL orchestrate the full subtitle pipeline: download content, strip BOM, loop registered `ISubtitleFormat` instances (TTML, WebVTT, SRT order), parse to cues via first matching format, emit as SRT, and return a `SubtitleResult`.

#### Scenario: Successful TTML subtitle
- **WHEN** the URL returns valid TTML content
- **THEN** the preparer SHALL match via `TtmlFormat.CanParse`, parse to cues, emit as SRT, and return `SubtitleResult.Succeeded` with the track and file path

#### Scenario: Successful WebVTT subtitle
- **WHEN** the URL returns valid WebVTT content
- **THEN** the preparer SHALL match via `WebVttFormat.CanParse`, parse to cues, emit as SRT, and return `SubtitleResult.Succeeded`

#### Scenario: Successful SRT subtitle
- **WHEN** the URL returns valid SRT content
- **THEN** the preparer SHALL match via `SrtFormat.CanParse`, parse to cues, emit as SRT, and return `SubtitleResult.Succeeded`

#### Scenario: HTTP error
- **WHEN** the URL returns a non-success HTTP status
- **THEN** the preparer SHALL return `SubtitleResult.Failed` with reason `DownloadFailed`

#### Scenario: Network failure
- **WHEN** the HTTP request fails due to a network error
- **THEN** the preparer SHALL return `SubtitleResult.Failed` with reason `DownloadFailed` and the exception message as detail

#### Scenario: Empty content
- **WHEN** the URL returns empty or whitespace-only content
- **THEN** the preparer SHALL return `SubtitleResult.Failed` with reason `EmptyContent`

#### Scenario: Unrecognized format
- **WHEN** no registered `ISubtitleFormat` returns true from `CanParse`
- **THEN** the preparer SHALL return `SubtitleResult.Failed` with reason `UnrecognizedFormat`

#### Scenario: Conversion produces no cues
- **WHEN** the parser produces zero cues from the content
- **THEN** the preparer SHALL return `SubtitleResult.Failed` with reason `ConversionFailed`

#### Scenario: BOM stripped before processing
- **WHEN** the downloaded content starts with a UTF-8 BOM
- **THEN** the preparer SHALL strip it before passing to format `CanParse` methods

### Requirement: SubtitlePreparer always emits SRT regardless of input format
All subtitle formats SHALL be converted to SRT for FFmpeg consumption.

#### Scenario: VTT converted to SRT
- **WHEN** a WebVTT subtitle is processed
- **THEN** the output file SHALL be `.srt` format, not `.vtt` passthrough

#### Scenario: TTML converted to SRT
- **WHEN** a TTML subtitle is processed
- **THEN** the output file SHALL be `.srt` format

#### Scenario: SRT validated and re-emitted
- **WHEN** an SRT subtitle is processed
- **THEN** the content SHALL be parsed to cues and re-emitted to ensure consistent formatting

### Requirement: SubtitlePreparer interface returns SubtitleResult
The `ISubtitlePreparer` interface SHALL return `SubtitleResult` instead of `string?`.

#### Scenario: Interface signature
- **WHEN** a consumer calls the preparer
- **THEN** the interface SHALL expose `Task<SubtitleResult> PrepareAsync(string url, string outputDirectory, string routeName, CancellationToken ct)`
