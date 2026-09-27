# Subtitle Formats

## Purpose

Concrete `ISubtitleFormat` implementations for TTML/EBU-TT-D, WebVTT, and SRT, plus the `SrtEmitter` for output and the format registry ordering rule.

## Requirements

### Requirement: TtmlFormat detects and parses TTML/EBU-TT-D
The `TtmlFormat` class SHALL implement `ISubtitleFormat` and handle all TTML variants from ARD, ZDF, and ORF.

#### Scenario: CanParse detects TTML
- **WHEN** the content is valid XML with a root element named `tt` (with or without namespace prefix)
- **THEN** `CanParse` SHALL return true
- **AND** SHALL cache the parsed `XDocument` for reuse by `Parse`

#### Scenario: CanParse rejects non-XML
- **WHEN** the content is not valid XML
- **THEN** `CanParse` SHALL return false without throwing

#### Scenario: Basic paragraph extraction
- **WHEN** a TTML document contains `<p>` elements with `begin` and `end` attributes
- **THEN** each non-empty paragraph SHALL become a `SubtitleCue` with parsed timestamps and extracted text

#### Scenario: Duration-based timing
- **WHEN** a `<p>` element has `begin` and `dur` attributes instead of `begin` and `end`
- **THEN** `Parse` SHALL compute `End` as `Begin + Duration`

#### Scenario: Namespaced and non-namespaced elements
- **WHEN** the document uses `tt:p` (ARD/ZDF) or plain `p` (ORF) elements
- **THEN** `Parse` SHALL handle both identically

#### Scenario: Nested spans and line breaks
- **WHEN** a `<p>` element contains `<span>` children or `<br/>` elements
- **THEN** `Parse` SHALL concatenate span text and insert newlines for `<br/>`

#### Scenario: EBU-TT-D offset normalization from metadata
- **WHEN** the document contains `documentStartOfProgramme` metadata (e.g. `10:00:00.000`)
- **THEN** `Parse` SHALL subtract that offset from all timestamps
- **AND** clamp negative results to zero

#### Scenario: Auto-detect offset from high timestamps
- **WHEN** no `documentStartOfProgramme` is present and the minimum `begin` exceeds 30 minutes
- **THEN** `Parse` SHALL use that minimum as the offset

#### Scenario: Timestamp formats
- **WHEN** timestamps use `HH:MM:SS.mmm`, `HH:MM:SS,mmm`, `HH:MM:SS`, or seconds-only (`123.456s`) format
- **THEN** `Parse` SHALL parse all formats correctly

#### Scenario: XDocument reused from CanParse
- **WHEN** `Parse` is called after a successful `CanParse`
- **THEN** `Parse` SHALL reuse the cached `XDocument` instead of re-parsing the string

### Requirement: WebVttFormat detects and parses WebVTT
The `WebVttFormat` class SHALL implement `ISubtitleFormat` and handle VTT from BR, SRF, and ARTE.

#### Scenario: CanParse detects WebVTT
- **WHEN** the content (after trimming) starts with `WEBVTT`
- **THEN** `CanParse` SHALL return true

#### Scenario: CanParse rejects non-VTT
- **WHEN** the content does not start with `WEBVTT`
- **THEN** `CanParse` SHALL return false

#### Scenario: Basic cue extraction
- **WHEN** content contains VTT cues with `HH:MM:SS.mmm --> HH:MM:SS.mmm` timestamps
- **THEN** each cue SHALL become a `SubtitleCue` with parsed timestamps and text

#### Scenario: Short timestamp format
- **WHEN** timestamps use `MM:SS.mmm` format (hours omitted)
- **THEN** `Parse` SHALL parse them as zero hours

#### Scenario: NOTE blocks ignored
- **WHEN** content contains `NOTE` blocks
- **THEN** `Parse` SHALL skip them

#### Scenario: STYLE blocks ignored
- **WHEN** content contains `STYLE` blocks with CSS
- **THEN** `Parse` SHALL skip them

#### Scenario: Cue settings ignored
- **WHEN** cue lines contain positioning settings after the timestamp (e.g., `position:10%`)
- **THEN** `Parse` SHALL ignore the settings and extract only timestamps

#### Scenario: Styling tags stripped
- **WHEN** cue text contains HTML-like tags (`<b>`, `<i>`, `<c.color>`)
- **THEN** `Parse` SHALL strip them and keep only plain text

### Requirement: SrtFormat detects and parses SRT
The `SrtFormat` class SHALL implement `ISubtitleFormat` and handle SubRip content.

#### Scenario: CanParse detects SRT
- **WHEN** the content starts with a digit and contains `-->`
- **THEN** `CanParse` SHALL return true

#### Scenario: Basic SRT parsing
- **WHEN** content contains numbered SRT entries with `HH:MM:SS,mmm --> HH:MM:SS,mmm` timestamps
- **THEN** each entry SHALL become a `SubtitleCue`

#### Scenario: Dot separator accepted
- **WHEN** SRT timestamps use `.` instead of `,` as millisecond separator
- **THEN** `Parse` SHALL accept both formats

#### Scenario: Multi-line text
- **WHEN** an SRT entry has multiple lines of text
- **THEN** `Parse` SHALL preserve line breaks in the `Text` field

#### Scenario: Blank entries skipped
- **WHEN** an SRT entry has empty or whitespace-only text
- **THEN** `Parse` SHALL skip it

### Requirement: SrtEmitter writes cues as SRT
The `SrtEmitter` SHALL convert a list of `SubtitleCue` entries into a valid SRT string. It is standalone, not part of `ISubtitleFormat`.

#### Scenario: Standard SRT output
- **WHEN** given a list of cues
- **THEN** the emitter SHALL produce numbered entries with `HH:MM:SS,mmm --> HH:MM:SS,mmm` timestamps
- **AND** entries SHALL be separated by blank lines

#### Scenario: Sequential numbering
- **WHEN** cues are emitted
- **THEN** they SHALL be numbered sequentially starting from 1

### Requirement: Format registry ordering
The pipeline SHALL try formats in a fixed order: TTML first (XML parse is definitive), WebVTT second, SRT last (loosest heuristic).

#### Scenario: TTML detected before SRT
- **WHEN** content is valid XML with a `tt` root
- **THEN** TtmlFormat SHALL match before SrtFormat gets a chance

#### Scenario: SRT is fallback
- **WHEN** content is not XML and not VTT but starts with a digit and contains `-->`
- **THEN** SrtFormat SHALL be the last format tried
