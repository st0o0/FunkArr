# Subtitle Preparer

## Purpose

Downloads subtitle content from a remote URL, detects the format by content sniffing, converts TTML/EBU-TT-D to SRT when needed, and writes a local file that FFmpeg can consume.

## Requirements

### Requirement: SubtitlePreparer downloads and converts subtitles
The SubtitlePreparer SHALL download subtitle content from a URL, detect its format, convert if necessary, and write a local SRT or VTT file. It SHALL return the local file path on success or null on failure.

#### Scenario: TTML/EBU-TT-D subtitle
- **WHEN** the URL returns XML content in TTML or EBU-TT-D format
- **THEN** the preparer SHALL parse the timed text elements and convert to SRT format
- **AND** write the SRT content to a `.srt` file in the specified output directory
- **AND** return the file path

#### Scenario: WebVTT subtitle
- **WHEN** the URL returns content starting with `WEBVTT`
- **THEN** the preparer SHALL write the content to a `.vtt` file in the specified output directory
- **AND** return the file path

#### Scenario: SRT subtitle
- **WHEN** the URL returns content in SRT format
- **THEN** the preparer SHALL write the content to a `.srt` file in the specified output directory
- **AND** return the file path

#### Scenario: Empty response
- **WHEN** the URL returns a successful HTTP status but empty content (0 bytes)
- **THEN** the preparer SHALL return null

#### Scenario: HTTP error
- **WHEN** the URL returns a non-success HTTP status
- **THEN** the preparer SHALL return null

#### Scenario: Network failure
- **WHEN** the HTTP request fails due to a network error
- **THEN** the preparer SHALL return null

#### Scenario: Unrecognized format
- **WHEN** the URL returns content that does not match any known subtitle format
- **THEN** the preparer SHALL return null

### Requirement: SubtitlePreparer detects format by content sniffing
The SubtitlePreparer SHALL detect the subtitle format by inspecting the content, not the URL pattern or content-type header.

#### Scenario: TTML detection
- **WHEN** the content starts with `<?xml` or `<tt`
- **THEN** the preparer SHALL treat it as TTML/EBU-TT-D

#### Scenario: WebVTT detection
- **WHEN** the content starts with `WEBVTT`
- **THEN** the preparer SHALL treat it as WebVTT

#### Scenario: SRT detection
- **WHEN** the content starts with a digit followed by a newline and a timestamp pattern (`-->`)
- **THEN** the preparer SHALL treat it as SRT

### Requirement: SubtitlePreparer converts TTML to SRT
The SubtitlePreparer SHALL convert TTML/EBU-TT-D XML to SubRip (SRT) format by extracting timed text paragraphs.

#### Scenario: Basic TTML paragraph conversion
- **WHEN** a TTML document contains `<p>` elements with `begin` and `end` attributes
- **THEN** each paragraph SHALL be converted to a numbered SRT entry with start time, end time, and text content

#### Scenario: TTML timestamp format
- **WHEN** TTML timestamps use `HH:MM:SS.mmm` or seconds-only (`123.456s`) format
- **THEN** the converter SHALL normalize to SRT timestamp format `HH:MM:SS,mmm`

#### Scenario: TTML with nested spans
- **WHEN** a `<p>` element contains nested `<span>` elements
- **THEN** the converter SHALL extract the text content from all nested spans and concatenate them

#### Scenario: TTML with line breaks
- **WHEN** a `<p>` element contains `<br/>` elements
- **THEN** the converter SHALL insert newlines in the SRT output

#### Scenario: TTML with HTML entities
- **WHEN** the text content contains XML entities (`&lt;`, `&gt;`, `&amp;`)
- **THEN** the converter SHALL decode them to plain text

#### Scenario: EBU-TT-D-Basic-DE profile
- **WHEN** the TTML document uses the EBU-TT-D-Basic-DE profile with namespaced elements (`tt:p`, `tt:span`)
- **THEN** the converter SHALL handle namespaced elements identically to non-namespaced ones

#### Scenario: ORF TTML variant
- **WHEN** the TTML document uses the ORF style (non-namespaced `<p>` under `<div>` under `<body>`)
- **THEN** the converter SHALL extract paragraphs from the same XML structure

### Requirement: SubtitlePreparer interface
The `ISubtitlePreparer` interface SHALL define a single async method for preparing subtitles.

#### Scenario: Interface signature
- **WHEN** a consumer needs to prepare a subtitle
- **THEN** the interface SHALL expose `Task<string?> PrepareAsync(string url, string outputDirectory, CancellationToken ct)`
- **AND** return the local file path on success or null when subtitles are unavailable

### Requirement: SubtitlePreparer uses HttpClient via DI
The `SubtitlePreparer` SHALL receive `HttpClient` through dependency injection for HTTP operations.

#### Scenario: DI registration
- **WHEN** the SubtitlePreparer is registered in DI
- **THEN** it SHALL use `IHttpClientFactory` for HttpClient creation
