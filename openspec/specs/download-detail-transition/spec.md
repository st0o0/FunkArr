# download-detail-transition Specification

## Purpose

Live state transition on the Download Detail page when a download completes or fails. Replaces the current "Not Found" behavior with a seamless transition to the completed/failed state, backed by a dedicated single-item history endpoint.

## Requirements

### Requirement: Single-item history endpoint

The API SHALL provide `GET /api/download/history/{id}` returning a single history item by download ID.

#### Scenario: Fetch completed item
- **WHEN** a client sends `GET /api/download/history/{id}` for a completed download
- **THEN** the API SHALL return the history item with status, title, quality, size, filePath, duration, completedAt, category, and failMessage (if failed)

#### Scenario: Item not found
- **WHEN** a client sends `GET /api/download/history/{id}` for an ID not in history
- **THEN** the API SHALL return HTTP 404

### Requirement: Detail page live transition

The Download Detail page SHALL seamlessly transition from active/queued state to completed/failed state when a download finishes, without showing "Not Found."

#### Scenario: Download completes while viewing detail
- **WHEN** the user is viewing an active download's detail page
- **AND** the download completes (item disappears from SSE queue stream)
- **THEN** the page SHALL fetch `GET /api/download/history/{id}`
- **AND** transition to show the completed state with all phases marked done

#### Scenario: Download fails while viewing detail
- **WHEN** the user is viewing an active download's detail page
- **AND** the download fails (item disappears from SSE queue stream)
- **THEN** the page SHALL fetch `GET /api/download/history/{id}`
- **AND** transition to show the failed state with the failure message

#### Scenario: History fetch retry on 404
- **WHEN** the item disappears from SSE and the history endpoint returns 404
- **THEN** the page SHALL retry the history fetch after 1-2 seconds
- **AND** show a loading state during the retry

#### Scenario: Direct navigation to completed download
- **WHEN** the user navigates directly to `/activity/{id}` for a completed download
- **THEN** the page SHALL find no queue item in SSE
- **AND** fetch `GET /api/download/history/{id}` to display the completed state

### Requirement: Completed state display

The Download Detail page in completed state SHALL display metadata from the history item and show all pipeline phases as done.

#### Scenario: Phase stepper in completed state
- **WHEN** the detail page shows a completed download
- **THEN** the phase stepper SHALL display all phases (Download, Remux, Move) as completed

#### Scenario: Completion metadata
- **WHEN** the detail page shows a completed download
- **THEN** the page SHALL display: file path, total size, duration, completion timestamp, channel, quality, and subtitles flag

#### Scenario: Failed state metadata
- **WHEN** the detail page shows a failed download
- **THEN** the page SHALL display the failure message and a Retry button
- **AND** the phase stepper SHALL show the failed phase highlighted

### Requirement: Transition feedback

The Download Detail page SHALL provide visual feedback when a download transitions from active to completed/failed.

#### Scenario: Completion banner
- **WHEN** a download transitions from active to completed while the user is viewing the detail page
- **THEN** a success banner SHALL appear briefly indicating the download completed successfully

#### Scenario: Failure banner
- **WHEN** a download transitions from active to failed while the user is viewing the detail page
- **THEN** an error banner SHALL appear indicating the download failed
