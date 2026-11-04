# activity-view Delta Specification

## MODIFIED Requirements

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

## REMOVED Requirements

### Requirement: Activity view tab navigation
**Reason**: Replaced by unified table with filter chips. Tabs caused the "vanishing download" problem where completing downloads moved from Queue tab to History tab with no transition.
**Migration**: Filter chips (All, Active, Queued, Completed, Failed) replace tabs. No tab state management needed.

### Requirement: Activity Active tab
**Reason**: Active downloads are now rows in the unified table, filtered by the "Active" chip.
**Migration**: Active download data (progress, speed, ETA) shown inline in the Status column of the table.

### Requirement: Activity Queued tab
**Reason**: Queued items are now rows in the unified table, filtered by the "Queued" chip.
**Migration**: Queued item data (title, channel, category, size) shown as table columns.

### Requirement: Activity History tab
**Reason**: History items are now rows in the unified table, filtered by "Completed" or "Failed" chips.
**Migration**: History table functionality (pagination, category filter, retry, delete) preserved in the unified table.

### Requirement: Activity search input
**Reason**: Replaced by new search that works across all statuses, not just history.
**Migration**: New search input filters all items (queue + history) by title, not just history.
