# activity-table Specification

## Purpose

Unified paginated table replacing the card-based queue layout and tab-based queue/history split. Merges active, queued, completed, and failed downloads into a single table with filter chips, search, and status-aware columns. Designed for monitoring 200+ downloads.

## Requirements

### Requirement: Unified download table

The Activity page SHALL render a single table displaying downloads across all states (active, queued, completed, failed). The table SHALL replace the previous Queue/History tab layout.

#### Scenario: Table columns for queued item
- **WHEN** the table renders a queued download
- **THEN** the row SHALL display columns: Position, Title, Status ("Queued"), Priority, Size, and an actions column

#### Scenario: Table columns for active item
- **WHEN** the table renders an active download
- **THEN** the row SHALL display: Position, Title, Status (phase label + progress percentage + speed), Priority, Size, and an actions column

#### Scenario: Table columns for completed item
- **WHEN** the table renders a completed download
- **THEN** the row SHALL display: Position (empty), Title, Status ("Completed"), Priority (empty), Size, and an actions column

#### Scenario: Table columns for failed item
- **WHEN** the table renders a failed download
- **THEN** the row SHALL display: Position (empty), Title, Status ("Failed"), Priority (empty), Size, and an actions column with a retry button

#### Scenario: Row click navigates to detail
- **WHEN** the user clicks a table row
- **THEN** the router SHALL navigate to `/activity/{id}`

### Requirement: Filter chips with counts

The Activity page SHALL display filter chips above the table: All, Active, Queued, Completed, Failed. Each chip SHALL display the count of items in that state. The "All" chip SHALL be selected by default.

#### Scenario: Filter chip counts
- **WHEN** the SSE stream reports 3 active and 197 queued items, and history contains 41 completed and 5 failed
- **THEN** the chips SHALL display "All 246", "Active 3", "Queued 197", "Completed 41", "Failed 5"

#### Scenario: Active filter selected
- **WHEN** the user selects the "Active" filter chip
- **THEN** the table SHALL show only items with status Processing from the SSE stream

#### Scenario: Completed filter selected
- **WHEN** the user selects the "Completed" filter chip
- **THEN** the table SHALL fetch `GET /api/downloads/history?start=0&limit=25` filtered to completed items and display the results

#### Scenario: All filter shows queue first
- **WHEN** the "All" filter is selected
- **THEN** active and queued items (from SSE) SHALL appear first, followed by paginated history items

### Requirement: Table pagination

The table SHALL paginate with 25 items per page. Pagination SHALL apply to history items (completed/failed). Active and queued items from SSE are not paginated.

#### Scenario: Page navigation
- **WHEN** history contains 100 completed items and the user is on page 1
- **THEN** the table SHALL show items 1-25 with pagination controls showing "1 2 3 4" and prev/next buttons

#### Scenario: Page change fetches history
- **WHEN** the user clicks page 3
- **THEN** the system SHALL fetch `GET /api/downloads/history?start=50&limit=25`

#### Scenario: Total count display
- **WHEN** pagination is active
- **THEN** the table SHALL display "Showing X-Y of Z" below the table

### Requirement: Search input

The Activity page SHALL display a search input above the filter chips. The search input SHALL filter items by title across all statuses.

#### Scenario: Search filters queue items
- **WHEN** the user types "tatort" in the search input
- **THEN** active and queued items from SSE SHALL be filtered client-side to those whose title contains "tatort" (case-insensitive)

#### Scenario: Search filters history items
- **WHEN** the user types "tatort" and the Completed filter is active
- **THEN** the system SHALL include the search term when fetching history and filter results by title

#### Scenario: Empty search shows all
- **WHEN** the search input is cleared
- **THEN** all items matching the current filter SHALL be shown

### Requirement: Additional filter dropdowns

The Activity page SHALL display optional filter dropdowns for Priority and Channel below the search input.

#### Scenario: Priority filter
- **WHEN** the user selects "High" from the Priority dropdown
- **THEN** only items with High priority SHALL be shown (applies client-side to queue items)

#### Scenario: Channel filter populated from queue data
- **WHEN** the Channel dropdown opens
- **THEN** it SHALL list distinct channel values from the current queue items

### Requirement: Inline row actions

Each table row SHALL have inline action buttons in the actions column. Available actions depend on item status.

#### Scenario: Queued item actions
- **WHEN** a row displays a queued item
- **THEN** the actions column SHALL show a Delete button

#### Scenario: Active item actions
- **WHEN** a row displays an active (processing) item
- **THEN** the actions column SHALL show a Delete (cancel) button

#### Scenario: Failed item actions
- **WHEN** a row displays a failed item
- **THEN** the actions column SHALL show Retry and Delete buttons

#### Scenario: Completed item actions
- **WHEN** a row displays a completed item
- **THEN** the actions column SHALL show a Delete button

#### Scenario: Delete queued item
- **WHEN** the user clicks Delete on a queued item
- **THEN** the system SHALL send `DELETE /api/downloads/queue/{id}` and show a success toast

#### Scenario: Delete history item
- **WHEN** the user clicks Delete on a completed or failed item
- **THEN** the system SHALL send `DELETE /api/downloads/history/{id}` and show a success toast

#### Scenario: Retry failed item
- **WHEN** the user clicks Retry on a failed item
- **THEN** the system SHALL send `POST /api/downloads/{id}/retry` and show a success toast
