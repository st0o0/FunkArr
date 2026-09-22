# Queue Pipeline Controls

## Purpose

Pause/resume controls and pipeline state display in the queue view header.

## Requirements

### Requirement: Pause/Resume button
The queue view SHALL display a Pause/Resume toggle button in the header area.

#### Scenario: Pause pipeline
- **WHEN** the pipeline is active and the user clicks "Pause"
- **THEN** `POST /api/downloads/pause` SHALL be called
- **AND** the button SHALL change to "Resume"

#### Scenario: Resume pipeline
- **WHEN** the pipeline is paused and the user clicks "Resume"
- **THEN** `POST /api/downloads/resume` SHALL be called
- **AND** the button SHALL change to "Pause"

### Requirement: Pipeline state display
The queue view SHALL visually indicate whether the pipeline is paused, active, or schedule-gated.

#### Scenario: Paused state
- **WHEN** `isPaused` is true
- **THEN** a "Paused" indicator SHALL be displayed prominently

#### Scenario: Schedule-gated state
- **WHEN** `isScheduleActive` is false and `nextWindow` is set
- **THEN** a schedule indicator SHALL show when the next download window opens
