# playwright-download-detail-tests Specification

## Purpose

Tests for the Download Detail page: active/completed/failed states, phase stepper rendering, live transition from active to completed/failed, direct URL navigation, and error handling.

## Requirements

### Requirement: Active download state

The detail page SHALL show live progress data for active downloads.

#### Scenario: Progress bar and speed
- **WHEN** viewing an active download at 67% with 12.4 MB/s
- **THEN** the page SHALL show a progress bar at 67%, speed "12.4 MB/s", and ETA

#### Scenario: Phase stepper current phase
- **WHEN** the download is in the Remuxing phase
- **THEN** the phase stepper SHALL show Download as done (green check), Remux as current (accent), Move as pending

#### Scenario: Channel and subtitle badges
- **WHEN** the download has channel "ARD" and subtitles enabled
- **THEN** both "ARD" and "SUB" badges SHALL be visible

#### Scenario: Force Start button visible
- **WHEN** viewing an active download
- **THEN** the Force Start button SHALL be visible

### Requirement: Completed download state

The detail page SHALL show completion metadata for finished downloads.

#### Scenario: Completed badge and phases
- **WHEN** viewing a completed download
- **THEN** a green "Completed" badge SHALL be visible and all 3 phase steps SHALL show green checkmarks

#### Scenario: Completion metadata
- **WHEN** viewing a completed download with file path, duration, and timestamp
- **THEN** all three metadata fields SHALL be visible with correct values

#### Scenario: No Force Start button
- **WHEN** viewing a completed download
- **THEN** the Force Start button SHALL NOT be visible

### Requirement: Failed download state

The detail page SHALL show failure details for failed downloads.

#### Scenario: Failed badge and error message
- **WHEN** viewing a failed download with message "Connection timeout"
- **THEN** a red "Failed" badge and the error message in a red box SHALL be visible

#### Scenario: Phase stepper shows failure
- **WHEN** viewing a failed download
- **THEN** the first phase step SHALL show "!" indicator

#### Scenario: Retry button visible
- **WHEN** viewing a failed download
- **THEN** the Retry button SHALL be visible

### Requirement: Live transition

The detail page SHALL transition seamlessly when an active download completes or fails.

#### Scenario: Active to completed transition
- **WHEN** viewing an active download AND the SSE stream removes the item
- **THEN** the page SHALL fetch the history item, show "Completed" badge, all phases green, and display a success toast

#### Scenario: Active to failed transition
- **WHEN** viewing an active download AND the SSE stream removes the item AND the history item has failed status
- **THEN** the page SHALL show "Failed" badge, error message, and display an error toast

#### Scenario: History fetch retry on 404
- **WHEN** the SSE item disappears AND the first history fetch returns 404
- **THEN** the page SHALL retry after ~1.5 seconds and succeed on the second attempt

### Requirement: Direct URL navigation

The detail page SHALL handle direct navigation without SSE history.

#### Scenario: Direct to completed download
- **WHEN** navigating directly to `/activity/{id}` for a completed download (not in SSE)
- **THEN** the page SHALL load the history item and show completed state

#### Scenario: Direct to non-existent download
- **WHEN** navigating to `/activity/{non-existent-id}`
- **THEN** the page SHALL show "Download not found" after loading

### Requirement: Action error handling

All action handlers SHALL show toast feedback on errors.

#### Scenario: Delete fails
- **WHEN** the user clicks Delete and the API returns an error
- **THEN** a toast with an error message SHALL appear and the page SHALL NOT navigate away

#### Scenario: Retry fails
- **WHEN** the user clicks Retry and the API returns an error
- **THEN** a toast with an error message SHALL appear

#### Scenario: Force Start fails
- **WHEN** the user clicks Force Start and the API returns an error
- **THEN** a toast with an error message SHALL appear
