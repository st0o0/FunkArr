## MODIFIED Requirements

### Requirement: History page table layout

The History page SHALL display download history as a table with columns for Title, Channel, Category, Size, Duration, Status, and Completed date. The table header (`<thead>`) SHALL be sticky (`sticky top-0 z-10`) so column labels remain visible when scrolling. Table rows SHALL use Level 3 interactive styling with `hover:bg-surface-elevated/60 transition-colors`.

#### Scenario: Table with completed downloads
- **WHEN** the history contains completed downloads
- **THEN** each row SHALL display the title, channel, category, size (formatted), download duration (formatted), a status indicator showing "Completed" with `status-ok` color, and the completion date/time

#### Scenario: Sticky header on scroll
- **WHEN** the user scrolls the history table vertically
- **THEN** the column headers SHALL remain fixed at the top of the scroll container

#### Scenario: Row hover state
- **WHEN** the user hovers over a history table row
- **THEN** the row background SHALL transition to `surface-elevated/60`

### Requirement: History page empty state

The History page SHALL display a structured empty state when no history records exist. The empty state SHALL include a clock/history icon, a title ("No download history"), and a descriptive subtitle.

#### Scenario: Empty history
- **WHEN** there are no completed or failed downloads
- **THEN** the page SHALL display a centered empty state with icon, title "No download history", and subtitle "Completed and failed downloads will appear here."

### Requirement: History page loading state

The History page SHALL display a skeleton table while the initial data fetch is in progress. The skeleton SHALL mimic the table layout with shimmer-animated placeholder rows.

#### Scenario: Initial loading
- **WHEN** the History page mounts and data has not yet loaded
- **THEN** a skeleton table with shimmer placeholder rows SHALL be displayed

#### Scenario: Data loaded
- **WHEN** the API response arrives
- **THEN** the skeleton SHALL be replaced with the actual table or the empty state

### Requirement: History page retry action

Failed download rows SHALL display a retry button. On successful retry, a toast notification SHALL be shown.

#### Scenario: Retry failed download
- **WHEN** the user clicks the retry button on a failed download row
- **THEN** the system SHALL send `POST /api/downloads/{id}/retry`
- **AND** a success toast SHALL display "Download re-queued"

#### Scenario: Retry error
- **WHEN** the retry API call fails
- **THEN** an error toast SHALL display the error message

### Requirement: History page delete action

Each history row SHALL have a delete action. On successful deletion, a toast notification SHALL be shown.

#### Scenario: Delete history entry
- **WHEN** the user clicks the delete button on a history row
- **THEN** the system SHALL send `DELETE /api/downloads/history/{id}`
- **AND** a success toast SHALL display "History entry deleted"

#### Scenario: Delete error
- **WHEN** the delete API call fails
- **THEN** an error toast SHALL display the error message
