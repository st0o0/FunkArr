# activity-view Specification

## Purpose

Unified Activity view combining active downloads, queued items, and download history into a single tabbed interface at `/activity`, replacing the separate `/queue` and `/history` routes.

## Requirements

### Requirement: Activity view route

The application SHALL register an `/activity` route rendering the Activity view within the AppLayout. The Activity view SHALL replace the separate `/queue` and `/history` routes.

#### Scenario: Navigation to activity page
- **WHEN** the user navigates to `/activity`
- **THEN** the Activity view SHALL render within the AppLayout

#### Scenario: Legacy route redirect from queue
- **WHEN** the user navigates to `/queue`
- **THEN** the router SHALL redirect to `/activity` with the Active tab selected

#### Scenario: Legacy route redirect from history
- **WHEN** the user navigates to `/history`
- **THEN** the router SHALL redirect to `/activity` with the History tab selected

### Requirement: Activity view tab navigation

The Activity view SHALL display three tabs: Active, Queued, and History. Tab state SHALL be a client-side reactive ref, not a route parameter. Switching tabs SHALL NOT cause a component re-mount or SSE reconnection.

#### Scenario: Default tab
- **WHEN** the Activity view mounts
- **THEN** the Active tab SHALL be selected by default

#### Scenario: Tab switching
- **WHEN** the user clicks the "History" tab
- **THEN** the History panel SHALL render and the Active panel SHALL be hidden
- **AND** no SSE reconnection SHALL occur

#### Scenario: Tab badge counts
- **WHEN** the SSE stream reports 3 active and 7 queued items
- **THEN** the Active tab SHALL display "Active 3" and the Queued tab SHALL display "Queued 7"

### Requirement: Activity Active tab

The Active tab SHALL display currently downloading items with progress bars, speed, percentage, ETA, and a cancel button per item. Items SHALL be grouped by series (existing QueueGroupCard behavior). An overall progress bar SHALL show aggregate progress.

#### Scenario: Active download display
- **WHEN** the Active tab is selected and 2 downloads are processing
- **THEN** each download SHALL show title, progress bar, percentage, speed, and ETA

#### Scenario: Cancel download
- **WHEN** the user clicks the cancel button on an active download
- **THEN** the system SHALL send `DELETE /api/downloads/queue/{id}` and show a success toast

#### Scenario: No active downloads
- **WHEN** no downloads are processing
- **THEN** an empty state SHALL display "No active downloads"

### Requirement: Activity Queued tab

The Queued tab SHALL display waiting downloads showing title, channel, category, and size without progress data. Each item SHALL have a cancel button.

#### Scenario: Queued items display
- **WHEN** the Queued tab is selected and 5 items are queued
- **THEN** each item SHALL show title, channel, category, and size without progress bars

#### Scenario: Cancel queued item
- **WHEN** the user clicks the cancel button on a queued item
- **THEN** the system SHALL send `DELETE /api/downloads/queue/{id}` and show a success toast

### Requirement: Activity History tab

The History tab SHALL display completed and failed downloads in a table with columns: Title, Quality, Size, Duration, Status, Completed date, and Actions (retry/delete). The table SHALL support pagination and category filtering. The History tab SHALL fetch data via `GET /api/downloads/history` with `start` and `limit` parameters.

#### Scenario: History table display
- **WHEN** the History tab is selected
- **THEN** the system SHALL fetch `GET /api/downloads/history?start=0&limit=25` and render a table

#### Scenario: Category filter
- **WHEN** the user selects a category from the filter dropdown
- **THEN** the system SHALL fetch history filtered by that category

#### Scenario: Category dropdown population
- **WHEN** the History tab mounts
- **THEN** the system SHALL fetch `GET /api/downloads/history/categories` to populate the category filter

#### Scenario: Pagination
- **WHEN** history has more than 25 items
- **THEN** pagination controls SHALL display with page range and prev/next buttons

#### Scenario: Retry failed download
- **WHEN** the user clicks retry on a failed download row
- **THEN** the system SHALL send `POST /api/downloads/{id}/retry` and show a success toast

#### Scenario: Delete history entry
- **WHEN** the user clicks delete on a history row
- **THEN** the system SHALL send `DELETE /api/downloads/history/{id}` and show a success toast

### Requirement: Activity search input

The Activity view SHALL display a search input above the tabs. The search input SHALL filter History tab results client-side by title. The search input SHALL NOT affect the Active or Queued tabs.

#### Scenario: Search filters history
- **WHEN** the user types "tatort" in the search input and the History tab is active
- **THEN** only history entries whose title contains "tatort" (case-insensitive) SHALL be shown

#### Scenario: Search on active tab
- **WHEN** the user types in the search input while the Active tab is selected
- **THEN** active downloads SHALL NOT be filtered

### Requirement: Activity SSE connection lifecycle

The Activity view SHALL mount the `useQueueStream` composable at the view level. The SSE connection SHALL remain open across tab switches. The connection SHALL be released when the Activity view unmounts.

#### Scenario: SSE survives tab switch
- **WHEN** the user switches from Active to History and back to Active
- **THEN** the SSE connection SHALL remain the same (no reconnect)

#### Scenario: SSE cleanup on unmount
- **WHEN** the user navigates away from the Activity view
- **THEN** the SSE connection SHALL be released
