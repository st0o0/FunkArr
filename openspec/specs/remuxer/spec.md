# Remuxer

## Purpose

Orchestrates the full media remux pipeline: subtitle preparation, FFmpeg execution with local inputs, and temp file cleanup. Replaces `IFfmpegRunner` as the `DownloadWorker`'s dependency.
## Requirements
### Requirement: Remuxer orchestrates subtitle preparation and FFmpeg execution
The Remuxer SHALL coordinate `ISubtitlePreparer` and `IFfmpegRunner` to produce a remuxed MKV file with optional subtitles.

#### Scenario: Download with subtitle URL
- **WHEN** `RunAsync` is called with a non-null subtitle URL
- **THEN** the Remuxer SHALL first call `ISubtitlePreparer.PrepareAsync` to obtain a local subtitle file
- **AND** then call `IFfmpegRunner.RunAsync` with the video URL and the local subtitle path
- **AND** return the `FfmpegResult` from the runner

#### Scenario: Download without subtitle URL
- **WHEN** `RunAsync` is called with a null subtitle URL
- **THEN** the Remuxer SHALL call `IFfmpegRunner.RunAsync` with the video URL and null subtitle path
- **AND** skip subtitle preparation entirely

#### Scenario: Subtitle preparation fails
- **WHEN** `ISubtitlePreparer.PrepareAsync` returns null (subtitle unavailable or conversion failed)
- **THEN** the Remuxer SHALL call `IFfmpegRunner.RunAsync` with null subtitle path
- **AND** the download SHALL proceed without subtitles

### Requirement: Remuxer cleans up temp subtitle files
The Remuxer SHALL delete the temporary subtitle file after FFmpeg completes, regardless of success or failure.

#### Scenario: Cleanup after successful download
- **WHEN** FFmpeg completes successfully and a temp subtitle file was created
- **THEN** the Remuxer SHALL delete the temp subtitle file

#### Scenario: Cleanup after failed download
- **WHEN** FFmpeg fails and a temp subtitle file was created
- **THEN** the Remuxer SHALL delete the temp subtitle file

#### Scenario: No cleanup when no subtitle
- **WHEN** no temp subtitle file was created (subtitle was null or preparation failed)
- **THEN** the Remuxer SHALL not attempt any file cleanup

### Requirement: Remuxer forwards progress updates
The Remuxer SHALL forward all progress updates from `IFfmpegRunner` to the caller without modification.

#### Scenario: Progress passthrough
- **WHEN** `IFfmpegRunner` reports a `ProgressUpdate`
- **THEN** the Remuxer SHALL invoke the caller's `onProgress` callback with the same update

### Requirement: Remuxer supports cancellation
The Remuxer SHALL observe the provided `CancellationToken` and propagate cancellation to both subtitle preparation and FFmpeg execution.

#### Scenario: Cancellation during subtitle preparation
- **WHEN** the CancellationToken is cancelled during subtitle preparation
- **THEN** the Remuxer SHALL cancel the preparation and return a cancelled result

#### Scenario: Cancellation during FFmpeg execution
- **WHEN** the CancellationToken is cancelled during FFmpeg execution
- **THEN** the Remuxer SHALL propagate cancellation to `IFfmpegRunner`

### Requirement: Remuxer interface
The `IRemuxer` interface SHALL match the current `IFfmpegRunner` method signature so the `DownloadWorker` can swap dependencies with minimal changes.

#### Scenario: Interface signature
- **WHEN** a consumer needs to remux media
- **THEN** the interface SHALL expose `Task<FfmpegResult> RunAsync(string videoUrl, string? subtitleUrl, string outputPath, Action<ProgressUpdate> onProgress, CancellationToken ct)`

### Requirement: Remuxer composes ISubtitlePreparer and IFfmpegRunner via DI
The `Remuxer` SHALL receive `ISubtitlePreparer`, `IFfmpegRunner`, and `IDataFiles` through constructor injection.

#### Scenario: DI registration
- **WHEN** the Remuxer is registered in DI
- **THEN** it SHALL be registered as `IRemuxer` with its dependencies resolved from the container

### Requirement: SubtitlePreparer SHALL detect XML via parsing not string matching
The SubtitlePreparer SHALL attempt `XDocument.Parse` on downloaded content to detect TTML/EBU-TT-D format, regardless of XML comments, BOMs, processing instructions, or namespace prefixes before the root element.

#### Scenario: EBU-TT-D with XML comment preamble
- **WHEN** subtitle content starts with `<!-- comment --><tt:tt xmlns:tt="http://www.w3.org/ns/ttml">`
- **THEN** the SubtitlePreparer SHALL detect it as TTML and route to TtmlToSrtConverter

#### Scenario: EBU-TT-D with XML prolog
- **WHEN** subtitle content starts with `<?xml version="1.0"?>` followed by a `tt` root element
- **THEN** the SubtitlePreparer SHALL detect it as TTML and route to TtmlToSrtConverter

#### Scenario: Unprefixed TTML root element
- **WHEN** subtitle content has an unprefixed `<tt>` root element in the TTML namespace
- **THEN** the SubtitlePreparer SHALL detect it as TTML and route to TtmlToSrtConverter

#### Scenario: Non-XML content falls through
- **WHEN** subtitle content is not valid XML (WEBVTT, SRT, or other)
- **THEN** `XDocument.Parse` SHALL fail and the SubtitlePreparer SHALL fall through to WEBVTT and SRT detection

#### Scenario: Empty conversion result logged
- **WHEN** TtmlToSrtConverter returns empty or whitespace despite non-empty input
- **THEN** the SubtitlePreparer SHALL log a warning with the subtitle URL

### Requirement: TtmlToSrtConverter SHALL normalize EBU-TT-D timestamp offsets
The converter SHALL detect and subtract programme clock offsets so that SRT timestamps start near zero, matching the video timeline.

#### Scenario: Offset from documentStartOfProgramme metadata
- **WHEN** the EBU-TT-D document contains `<ebuttm:documentStartOfProgramme>10:00:00.000</ebuttm:documentStartOfProgramme>`
- **THEN** the converter SHALL subtract `10:00:00.000` from all begin and end timestamps

#### Scenario: Auto-detect offset from minimum begin
- **WHEN** the document does not contain `documentStartOfProgramme` metadata
- **AND** the minimum `begin` timestamp across all paragraphs exceeds 30 minutes
- **THEN** the converter SHALL use that minimum as the offset and subtract it from all timestamps

#### Scenario: No offset when timestamps start near zero
- **WHEN** the minimum `begin` timestamp is less than or equal to 30 minutes
- **AND** no `documentStartOfProgramme` metadata is present
- **THEN** the converter SHALL not apply any offset

#### Scenario: Timestamps clamped to non-negative
- **WHEN** subtracting the offset would produce a negative timestamp
- **THEN** the converter SHALL clamp the timestamp to `00:00:00,000`

#### Scenario: 20-hour offset handled
- **WHEN** the EBU-TT-D document uses a 20-hour offset (documented EBU convention)
- **THEN** the converter SHALL handle it identically to the 10-hour case

