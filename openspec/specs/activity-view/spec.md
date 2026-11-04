# activity-view Specification

## Purpose

Unified Activity view combining active downloads, queued items, and download history into a single unified table with filter chips at `/activity`, replacing the separate `/queue` and `/history` routes.

## Requirements

### Requirement: Activity view route

The application SHALL register an `/activity` route rendering the Activity view within the AppLayout. The Activity view SHALL replace the separate `/queue` and `/history` routes.

#### Scenario: Navigation to activity page
- **WHEN** the user navigates to `/activity`
- **THEN** the Activity view SHALL render within the AppLayout

#### Scenario: Legacy route redirect from queue
- **WHEN** the user navigates to `/queue`
- **THEN** the router SHALL redirect to `/activity`

#### Scenario: Legacy route redirect from history
- **WHEN** the user navigates to `/history`
- **THEN** the router SHALL redirect to `/activity`

### Requirement: Activity view layout

The Activity view SHALL display a search input, filter chips, optional filter dropdowns, and a unified paginated table. There SHALL be no tab navigation. The view SHALL use the `useQueueStream` composable for real-time queue data and REST calls for history data.

#### Scenario: Default state
- **WHEN** the Activity view mounts
- **THEN** the "All" filter chip SHALL be selected by default
- **AND** the table SHALL show active and queued items from SSE followed by paginated history

#### Scenario: View layout order
- **WHEN** the Activity view renders
- **THEN** the layout order SHALL be: search input, filter chips with counts, filter dropdowns (Priority, Channel), table, pagination controls

### Requirement: Activity SSE connection lifecycle

The Activity view SHALL mount the `useQueueStream` composable at the view level. The SSE connection SHALL remain open while the Activity view is mounted. The connection SHALL be released when the Activity view unmounts.

#### Scenario: SSE cleanup on unmount
- **WHEN** the user navigates away from the Activity view
- **THEN** the SSE connection SHALL be released
