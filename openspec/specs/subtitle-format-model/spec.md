# Subtitle Format Model

## Purpose

Defines the core data model for subtitle processing: the `ISubtitleFormat` interface, `SubtitleCue` intermediate representation, `SubtitleTrack` metadata container, and `SubtitleResult` discriminated union for pipeline outcomes.

## Requirements

### Requirement: ISubtitleFormat interface defines self-contained format classes
The `ISubtitleFormat` interface SHALL define a contract where each format handles its own detection and parsing.

#### Scenario: Interface members
- **WHEN** a subtitle format is implemented
- **THEN** it SHALL expose `string Name` (human-readable), `bool CanParse(string content)` (detection), and `List<SubtitleCue> Parse(string content)` (parsing)

#### Scenario: CanParse is non-destructive
- **WHEN** `CanParse` is called
- **THEN** it SHALL return true if the content matches this format, false otherwise
- **AND** it SHALL NOT throw exceptions for non-matching content

### Requirement: SubtitleCue represents a single timed text entry
The `SubtitleCue` record SHALL carry a start time, end time, and text content as the intermediate representation between parsing and emission.

#### Scenario: Cue structure
- **WHEN** a subtitle format produces a cue
- **THEN** it SHALL contain `Start` (TimeSpan), `End` (TimeSpan), and `Text` (string)
- **AND** `Text` SHALL not be null or whitespace

### Requirement: SubtitleTrack carries parsed subtitle data with metadata
The `SubtitleTrack` record SHALL combine parsed cues with format and language metadata.

#### Scenario: Track structure
- **WHEN** a subtitle is successfully parsed
- **THEN** the `SubtitleTrack` SHALL contain `Format` (string, the format name), `Language` (string, ISO 639-2), and `Cues` (IReadOnlyList of SubtitleCue)
- **AND** `Cues` SHALL contain at least one entry

#### Scenario: Default language
- **WHEN** no language information is available from the source
- **THEN** `Language` SHALL default to `"deu"`

### Requirement: SubtitleResult discriminates success, failure, and unavailability
The `SubtitleResult` abstract record SHALL have three concrete cases to distinguish outcomes.

#### Scenario: Successful preparation
- **WHEN** a subtitle is downloaded, parsed, and emitted to a file
- **THEN** the result SHALL be `SubtitleResult.Succeeded` containing the `SubtitleTrack` and the local file path

#### Scenario: Failed preparation
- **WHEN** a subtitle download succeeds but parsing or conversion fails
- **THEN** the result SHALL be `SubtitleResult.Failed` containing a `SubtitleFailureReason` and optional detail string

#### Scenario: Unavailable subtitle
- **WHEN** the subtitle URL returns HTTP 404, empty content, or the URL was null
- **THEN** the result SHALL be `SubtitleResult.Unavailable`

### Requirement: SubtitleFailureReason identifies failure causes
The `SubtitleFailureReason` enum SHALL identify why subtitle preparation failed.

#### Scenario: Defined reasons
- **WHEN** subtitle preparation fails
- **THEN** the reason SHALL be one of `DownloadFailed`, `EmptyContent`, `UnrecognizedFormat`, or `ConversionFailed`
