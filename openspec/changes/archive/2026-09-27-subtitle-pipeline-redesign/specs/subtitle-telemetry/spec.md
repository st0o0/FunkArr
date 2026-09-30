## ADDED Requirements

### Requirement: Subtitle operations are instrumented on the Download meter
The existing `FunkArr.Download` `Telemetry` class SHALL expose subtitle-specific instruments.

#### Scenario: Subtitle counter
- **WHEN** a subtitle preparation completes (success, failure, or unavailable)
- **THEN** `funkarr.download.subtitle_total` counter SHALL be incremented
- **AND** tagged with `status` (succeeded, failed, unavailable) and `format` (ttml, vtt, srt, unknown)

#### Scenario: Subtitle duration histogram
- **WHEN** a subtitle preparation completes
- **THEN** `funkarr.download.subtitle_duration_seconds` histogram SHALL record the elapsed time

### Requirement: Telemetry records format distribution
The subtitle counter SHALL track which formats are encountered.

#### Scenario: TTML format recorded
- **WHEN** a TTML subtitle is processed
- **THEN** the counter SHALL be incremented with tag `format=ttml`

#### Scenario: Unknown format recorded
- **WHEN** an unrecognized format is encountered
- **THEN** the counter SHALL be incremented with tag `format=unknown` and `status=failed`

### Requirement: SubtitlePreparer logs distinct outcomes
The preparer SHALL log at appropriate levels for each outcome.

#### Scenario: Success logged at debug
- **WHEN** subtitle preparation succeeds
- **THEN** a debug-level log entry SHALL include the URL and detected format

#### Scenario: Failure logged at warning
- **WHEN** subtitle preparation fails
- **THEN** a warning-level log entry SHALL include the URL, failure reason, and detail
