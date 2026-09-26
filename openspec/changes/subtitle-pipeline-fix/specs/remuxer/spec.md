## MODIFIED Requirements

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

## ADDED Requirements

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
