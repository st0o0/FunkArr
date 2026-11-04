# download-queue-ui Delta Specification

## MODIFIED Requirements

### Requirement: Global SSE composable
The application SHALL provide a `useQueueStream` composable that connects to the SSE endpoint and exposes reactive queue state. The composable SHALL NOT include drag-buffering logic (`isDragging`, `flushBuffer`).

#### Scenario: SSE connection on Activity mount
- **WHEN** the Activity view mounts
- **THEN** `useQueueStream` SHALL open an EventSource connection to `/api/downloads/queue/stream`

#### Scenario: Reactive state updates
- **WHEN** the SSE stream receives a `queue` event
- **THEN** the composable SHALL parse the JSON data and update its reactive `items` ref

#### Scenario: Composable shared state
- **WHEN** multiple components within the Activity view call `useQueueStream`
- **THEN** they SHALL share the same EventSource connection and reactive state

## REMOVED Requirements

### Requirement: Queue view layout
**Reason**: Replaced by the unified activity-table. The two-tab layout with priority-bucketed queue sections is replaced by a single paginated table with filter chips.
**Migration**: Activity table (activity-table spec) replaces this layout entirely.

### Requirement: Priority bucket sections
**Reason**: Priority buckets with section headers are replaced by a Priority column in the unified table. At 200+ items, bucket sections are unusable.
**Migration**: Priority is visible as a table column. Items are sorted by position, not grouped by priority.

### Requirement: Priority badge on queue cards
**Reason**: Queue cards no longer exist. Priority is a table column.
**Migration**: Priority column in the unified table.

### Requirement: Active download phase label
**Reason**: Phase labels on ActiveDownloadCard are replaced by the Status column in the table, which shows the current phase and progress inline.
**Migration**: Status column displays phase label + progress percentage for active items.
