## REMOVED Requirements

### Requirement: Queue page route
**Reason**: The standalone `/queue` route is removed. Queue functionality is absorbed into the Activity view's Active and Queued tabs.
**Migration**: The `/queue` route SHALL redirect to `/activity`. The Queue.vue file SHALL be deleted.

#### Scenario: Legacy queue route redirect
- **WHEN** the user navigates to `/queue`
- **THEN** the router SHALL redirect to `/activity`

### Requirement: Queue page displays active downloads as cards
**Reason**: Absorbed into the Activity view's Active tab (activity-view capability).
**Migration**: Active download cards render in the Activity view's Active tab with the same card layout and progress visualization.

#### Scenario: Active downloads in Activity view
- **WHEN** the user views the Active tab in Activity
- **THEN** active downloads display as cards with progress bars, speed, and ETA

### Requirement: Queue page displays queued items as cards
**Reason**: Absorbed into the Activity view's Queued tab (activity-view capability).
**Migration**: Queued items render in the Activity view's Queued tab.

#### Scenario: Queued items in Activity view
- **WHEN** the user views the Queued tab in Activity
- **THEN** queued items display as cards without progress data

### Requirement: Queue page shows empty state
**Reason**: Absorbed into the Activity view's Active tab empty state.
**Migration**: Empty state renders in the Active tab when no downloads are processing.

#### Scenario: Empty state in Activity Active tab
- **WHEN** no downloads are processing
- **THEN** the Active tab SHALL show "No active downloads"

### Requirement: Queue page cancel/delete action
**Reason**: Absorbed into the Activity view's Active and Queued tabs.
**Migration**: Cancel buttons appear on items in both Active and Queued tabs.

#### Scenario: Cancel in Activity view
- **WHEN** the user cancels a download in the Activity view
- **THEN** the system SHALL send `DELETE /api/downloads/queue/{id}` and show a toast

### Requirement: Queue page loading state
**Reason**: Absorbed into the Activity view SSE lifecycle.
**Migration**: Loading state is handled by the Activity view's SSE composable.

#### Scenario: Loading in Activity view
- **WHEN** the Activity view mounts and no SSE data has arrived
- **THEN** skeleton placeholders SHALL display

### Requirement: Queue page summary footer
**Reason**: Absorbed into the Activity view's tab badge counts and overall status.
**Migration**: Item counts display as tab badges (Active 3, Queued 7).

#### Scenario: Counts as tab badges
- **WHEN** the SSE stream has 3 active and 7 queued items
- **THEN** the Activity tabs SHALL show "Active 3" and "Queued 7"

## MODIFIED Requirements

### Requirement: Global SSE composable
The application SHALL provide a `useQueueStream` composable that connects to the SSE endpoint and exposes reactive queue state. This composable is unchanged and remains shared.

#### Scenario: SSE connection on Activity mount
- **WHEN** the Activity view mounts
- **THEN** `useQueueStream` SHALL open an EventSource connection to `/api/downloads/queue/stream`

#### Scenario: Reactive state updates
- **WHEN** the SSE stream receives a `queue` event
- **THEN** the composable SHALL parse the JSON data and update its reactive `items` ref

#### Scenario: Composable shared state
- **WHEN** multiple components within the Activity view call `useQueueStream`
- **THEN** they SHALL share the same EventSource connection and reactive state

### Requirement: Dashboard active downloads widget
The Overview page (formerly Dashboard) SHALL include a compact "Active Downloads" section showing active downloads with progress bars. The widget SHALL link to the Activity view instead of the Queue page.

#### Scenario: View all link target
- **WHEN** the active downloads widget renders
- **THEN** it SHALL include a "View All" link navigating to `/activity`

#### Scenario: Widget with active downloads
- **WHEN** there are active downloads in the SSE stream
- **THEN** the widget SHALL display each active download with title, percentage, speed, and a compact progress bar
