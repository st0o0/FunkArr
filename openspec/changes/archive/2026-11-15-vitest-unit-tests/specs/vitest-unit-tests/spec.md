# vitest-unit-tests Specification

## Purpose

Vitest unit test suite covering pure frontend logic: release title parsing, formatting utilities, activity row mapping, phase stepper state, ruleset matching, enum lookups, queue grouping, and toast behavior.

## Requirements

### Requirement: Release title parsing tests

The test suite SHALL cover all code paths of `parseReleaseName()` including edge cases.

#### Scenario: Standard SxxExx format
- **WHEN** parsing "Tatort.S2026E22.Tatort.Angezählt.GERMAN.1080p.WEB.h264-FunkArr"
- **THEN** the result SHALL contain series "Tatort", season "2026", episode "22", quality "1080p"

#### Scenario: Folge notation
- **WHEN** parsing "In.aller.Freundschaft.S10E30.Folge.408.Weiterziehen.GERMAN.720p.WEB.h264-FunkArr"
- **THEN** the result SHALL correctly identify the series name and episode

#### Scenario: No quality info
- **WHEN** parsing a title without quality markers
- **THEN** quality SHALL be null

#### Scenario: Empty or malformed input
- **WHEN** parsing an empty string or non-standard format
- **THEN** the function SHALL return a fallback result without throwing

### Requirement: Formatting utility tests

The test suite SHALL cover boundary conditions for all format functions.

#### Scenario: formatSize boundaries
- **WHEN** formatting 0, 1023, 1024, 1048576, and 1073741824 bytes
- **THEN** the results SHALL be "0 B", "1023 B", "1.0 KB", "1.0 MB", and "1.0 GB" respectively

#### Scenario: formatSpeed
- **WHEN** formatting 12400000 bytes/second
- **THEN** the result SHALL be "11.8 MB/s"

#### Scenario: formatDuration edge cases
- **WHEN** formatting 0 seconds and 3661 seconds
- **THEN** the results SHALL be "0s" and "1h 1m 1s" respectively

### Requirement: Activity row mapping tests

The test suite SHALL verify correct mapping from API types to display types for all 4 download states.

#### Scenario: Active download row
- **WHEN** mapping a QueueItem with status Processing and phase VideoDownload
- **THEN** type SHALL be "active", statusLabel SHALL be "Downloading", priority SHALL be localized

#### Scenario: Queued download row
- **WHEN** mapping a QueueItem with status Queued
- **THEN** type SHALL be "queued", statusDot SHALL be "bg-text-muted"

#### Scenario: Completed history row
- **WHEN** mapping a HistoryItem with status Completed
- **THEN** type SHALL be "completed", statusDot SHALL be "bg-status-ok", position SHALL be null

#### Scenario: Failed history row
- **WHEN** mapping a HistoryItem with status Failed and a failMessage
- **THEN** type SHALL be "failed", failMessage SHALL be preserved, statusDot SHALL be "bg-status-fail"

### Requirement: Phase stepper state tests

The test suite SHALL verify phase stepper computation for active, completed, and failed downloads.

#### Scenario: Active download at Remuxing phase
- **WHEN** computing stepper for phase 1 (Remuxing) with active status
- **THEN** step 0 (Download) SHALL be done, step 1 (Remux) SHALL be current, step 2 (Move) SHALL be pending

#### Scenario: Completed download
- **WHEN** computing stepper for a completed download
- **THEN** all 3 steps SHALL be done with green checkmark classes

#### Scenario: Failed download
- **WHEN** computing stepper for a failed download
- **THEN** step 0 SHALL show failure indicator

### Requirement: Ruleset matcher tests

The test suite SHALL cover all 5 matching strategies and error safety.

#### Scenario: SeasonEpisode strategy matches
- **WHEN** matching a candidate with title "Tatort S2026E22" using SeasonEpisode strategy
- **THEN** the match SHALL extract season "2026" and episode "22"

#### Scenario: Invalid regex does not throw
- **WHEN** a rule contains an invalid regex pattern
- **THEN** the matcher SHALL return no match without throwing

### Requirement: Enum map tests

The test suite SHALL verify all name-lookup functions return correct labels and handle unknown values.

#### Scenario: Known enum value
- **WHEN** looking up quality name for value 1080
- **THEN** the result SHALL be "1080p"

#### Scenario: Unknown enum value
- **WHEN** looking up a name for an unregistered value
- **THEN** the result SHALL be the stringified number

### Requirement: Grouped queue tests

The test suite SHALL verify series grouping logic using mocked queue stream data.

#### Scenario: Groups by series
- **WHEN** the queue contains 3 items from 2 different series
- **THEN** 2 groups SHALL be created with correct item counts

#### Scenario: Active items sorted first
- **WHEN** a group contains both active and queued items
- **THEN** active items SHALL appear before queued items

### Requirement: Toast tests

The test suite SHALL verify toast cap and auto-removal behavior.

#### Scenario: Max 3 toasts
- **WHEN** 4 toasts are added
- **THEN** only 3 SHALL be present (oldest removed)

#### Scenario: Auto-remove after timeout
- **WHEN** a toast is added and 3 seconds elapse (via fake timers)
- **THEN** the toast SHALL be automatically removed
