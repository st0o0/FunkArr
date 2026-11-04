# playwright-activity-tests Specification

## Purpose

Comprehensive tests for the Activity page: unified table, filter chips, search, dropdowns, pagination, inline actions, pipeline controls, and edge cases.

## Requirements

### Requirement: Table rendering by item type

The Activity table SHALL render different columns and formatting depending on item type.

#### Scenario: Active download row
- **WHEN** the SSE stream reports an active download
- **THEN** the table row SHALL show position, title with channel below, phase label with progress bar and percentage/speed, priority, size, and a delete button

#### Scenario: Queued download row
- **WHEN** the SSE stream reports a queued download
- **THEN** the table row SHALL show position, title with channel, a dot with "Queued" label, priority, size, and a delete button

#### Scenario: Completed download row
- **WHEN** history contains a completed download
- **THEN** the table row SHALL show no position, title, green dot with "Completed", no priority, size, and a delete button

#### Scenario: Failed download row
- **WHEN** history contains a failed download
- **THEN** the table row SHALL show title with expandable error message, red dot with "Failed", retry button, and delete button

#### Scenario: Row click navigates to detail
- **WHEN** the user clicks a table row
- **THEN** the browser SHALL navigate to `/activity/{downloadId}`

### Requirement: Filter chips with correct counts

Filter chips SHALL display accurate counts from SSE and history stats.

#### Scenario: Counts reflect data sources
- **WHEN** SSE reports 2 active and 5 queued, and history stats show 10 completed and 3 failed
- **THEN** chips SHALL show: All 20, Active 2, Queued 5, Completed 10, Failed 3

#### Scenario: Active filter shows only active items
- **WHEN** the user clicks the "Active" chip
- **THEN** the table SHALL show only active (processing) items from SSE

#### Scenario: Failed filter shows only failed items
- **WHEN** the user clicks the "Failed" chip
- **THEN** the table SHALL show only failed items from history

#### Scenario: Filter change resets pagination
- **WHEN** the user is on page 3 of "All" and clicks "Completed"
- **THEN** pagination SHALL reset to page 1

### Requirement: Search filtering

The search input SHALL filter items across queue and history by title.

#### Scenario: Search filters queue items client-side
- **WHEN** the user types "tatort" in the search input
- **THEN** only queue items whose title contains "tatort" (case-insensitive) SHALL be visible

#### Scenario: Clear search shows all items
- **WHEN** the search input is cleared
- **THEN** all items matching the current filter SHALL reappear

#### Scenario: Search with zero results
- **WHEN** the search term matches no items
- **THEN** the empty state SHALL be shown

### Requirement: Filter dropdowns

Priority, channel, and category dropdowns SHALL filter items appropriately.

#### Scenario: Priority filter on queue items
- **WHEN** the user selects "High" from the priority dropdown
- **THEN** only high-priority queue items SHALL be visible

#### Scenario: Channel filter populated from SSE data
- **WHEN** SSE reports items from ARD and ZDF
- **THEN** the channel dropdown SHALL contain "ARD" and "ZDF" options

#### Scenario: Category dropdown visibility
- **WHEN** the filter is set to "Active" or "Queued"
- **THEN** the category dropdown SHALL NOT be visible

#### Scenario: Category dropdown visible on history filters
- **WHEN** the filter is set to "All", "Completed", or "Failed"
- **THEN** the category dropdown SHALL be visible with categories from the API

### Requirement: Pagination

Pagination SHALL apply only to history-showing filters and display correct ranges.

#### Scenario: No pagination on queue-only filters
- **WHEN** the "Active" or "Queued" filter is selected
- **THEN** no pagination controls SHALL be visible

#### Scenario: Pagination on history filters
- **WHEN** the "Completed" filter is selected and history has 50 items
- **THEN** pagination SHALL show "1-25 of 50" with Next enabled and Prev disabled

#### Scenario: Page navigation
- **WHEN** the user clicks "Next"
- **THEN** the table SHALL show the next page of history items

### Requirement: Inline actions

Each row SHALL have appropriate action buttons that trigger API calls with toast feedback.

#### Scenario: Delete queue item
- **WHEN** the user clicks delete on a queued item
- **THEN** the system SHALL call `DELETE /api/downloads/queue/{id}` and show a success toast

#### Scenario: Delete history item
- **WHEN** the user clicks delete on a completed item
- **THEN** the system SHALL call `DELETE /api/downloads/history/{id}`, show a success toast, and re-fetch history

#### Scenario: Retry failed item
- **WHEN** the user clicks retry on a failed item
- **THEN** the system SHALL call `POST /api/downloads/{id}/retry`, show a success toast, and re-fetch history

### Requirement: Pipeline controls

The pause/resume button SHALL toggle the pipeline state.

#### Scenario: Pause pipeline
- **WHEN** the user clicks "Pause"
- **THEN** the system SHALL call `POST /api/downloads/pause` and show a "Pipeline paused" toast

#### Scenario: Resume pipeline
- **WHEN** the pipeline is paused and the user clicks "Resume"
- **THEN** the system SHALL call `POST /api/downloads/resume` and show a "Pipeline resumed" toast

#### Scenario: Paused badge visible
- **WHEN** the SSE stream reports `isPaused: true`
- **THEN** a "Paused" badge SHALL be visible in the header

### Requirement: Empty states

The page SHALL show appropriate empty states when no data matches.

#### Scenario: No downloads at all
- **WHEN** SSE reports 0 items and history is empty
- **THEN** the empty state SHALL show "No downloads" with a descriptive message

#### Scenario: Speed badge visibility
- **WHEN** no downloads are active (totalSpeed is 0)
- **THEN** the speed badge SHALL NOT be visible
