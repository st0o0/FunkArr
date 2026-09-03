# Download History UI

## Purpose

History page showing completed and failed downloads in a table layout with pagination, retry/delete actions, and status indicators.

## Requirements

### Requirement: History page route
The application SHALL register a `/history` route rendering the History view.

#### Scenario: Navigation to history page
- **WHEN** the user navigates to `/history`
- **THEN** the History view SHALL render within the AppLayout

### Requirement: History page table layout
The History page SHALL display download history as a table with columns for Title, Channel, Category, Size, Duration, Status, and Completed date.

#### Scenario: Table with completed downloads
- **WHEN** the history contains completed downloads
- **THEN** each row SHALL display the title, channel, category, size (formatted), download duration (formatted), a status indicator showing "Completed" with `status-ok` color, and the completion date/time

#### Scenario: Table with failed downloads
- **WHEN** the history contains failed downloads
- **THEN** each row SHALL display the title, channel, category, size (formatted), no duration, a status indicator showing "Failed" with `status-fail` color, the fail message as a tooltip or secondary text, and the failure date/time

### Requirement: History page empty state
The History page SHALL display an empty state when no history records exist.

#### Scenario: Empty history
- **WHEN** there are no completed or failed downloads
- **THEN** the page SHALL display a message indicating the history is empty

### Requirement: History page pagination
The History page SHALL paginate results using `start` and `limit` query parameters, with navigation controls.

#### Scenario: Pagination controls
- **WHEN** the history has more items than the page limit (25)
- **THEN** the page SHALL display pagination controls showing current range ("1-25 of 142") and previous/next buttons

#### Scenario: Next page navigation
- **WHEN** the user clicks the next page button
- **THEN** the system SHALL fetch `GET /api/downloads/history?start=25&limit=25`
- **AND** update the table with the next page of results

#### Scenario: URL reflects pagination state
- **WHEN** the user navigates to page 2
- **THEN** the URL SHALL update to `/history?page=2`
- **AND** refreshing the page SHALL load page 2

### Requirement: History page retry action
Failed download rows SHALL display a retry button.

#### Scenario: Retry failed download
- **WHEN** the user clicks the retry button on a failed download row
- **THEN** the system SHALL send `POST /api/downloads/{id}/retry`
- **AND** on success, the item SHALL be removed from the history table
- **AND** the queue count in the SSE stream SHALL reflect the re-queued item

### Requirement: History page delete action
Each history row SHALL have a delete action.

#### Scenario: Delete history entry
- **WHEN** the user clicks the delete button on a history row
- **THEN** the system SHALL send `DELETE /api/downloads/history/{id}`
- **AND** on success, the row SHALL be removed from the table

### Requirement: History page category filter
The History page SHALL support filtering by category via a dropdown or toggle.

#### Scenario: Filter by category
- **WHEN** the user selects category "sonarr" from the filter
- **THEN** the system SHALL fetch `GET /api/downloads/history?category=sonarr&start=0&limit=25`
- **AND** display only matching items

#### Scenario: Clear filter
- **WHEN** the user clears the category filter
- **THEN** the system SHALL fetch all history items without a category filter

### Requirement: Duration formatting
Download duration SHALL be formatted as human-readable time.

#### Scenario: Duration in minutes and seconds
- **WHEN** `downloadTimeSeconds` is 185
- **THEN** it SHALL display as "3m 05s"

#### Scenario: Duration in hours
- **WHEN** `downloadTimeSeconds` is 7320
- **THEN** it SHALL display as "2h 02m"

### Requirement: Completed date formatting
The completed date SHALL be formatted as a relative or absolute date/time.

#### Scenario: Recent completion
- **WHEN** the download completed within the last 24 hours
- **THEN** the date SHALL display as relative time (e.g., "2 hours ago")

#### Scenario: Older completion
- **WHEN** the download completed more than 24 hours ago
- **THEN** the date SHALL display as absolute date and time (e.g., "01.09.2026 20:15")

### Requirement: History data fetching
The History page SHALL fetch data on mount and after actions (delete, retry) using standard fetch calls, not SSE.

#### Scenario: Initial data load
- **WHEN** the History page mounts
- **THEN** it SHALL fetch `GET /api/downloads/history?start=0&limit=25`

#### Scenario: Refetch after action
- **WHEN** a delete or retry action completes successfully
- **THEN** the page SHALL refetch the current page of history data
