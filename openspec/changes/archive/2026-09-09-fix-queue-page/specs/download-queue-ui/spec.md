# download-queue-ui (delta)

## MODIFIED Requirements

### Requirement: Queue SSE stream reconnects reliably

The `useQueueStream` composable SHALL reconnect the EventSource when the current instance is in CLOSED state. The `connect()` function SHALL check `readyState` and replace stale connections instead of silently returning.

#### Scenario: Navigate Dashboard to Queue via SPA router

- **WHEN** user navigates from Dashboard to Queue via sidebar link
- **THEN** the Queue page SHALL display active downloads without requiring a page refresh

#### Scenario: EventSource in CLOSED state

- **WHEN** `connect()` is called and the existing EventSource has `readyState === EventSource.CLOSED`
- **THEN** the stale EventSource SHALL be closed and a new one created

### Requirement: Single-item groups render without group wrapper

The Queue view SHALL render downloads in groups only when a series has multiple items in the queue. Single-item groups SHALL render the QueueCard directly without the collapsible group header.

#### Scenario: Series with one download

- **WHEN** a series has exactly 1 item in the queue
- **THEN** it SHALL render as a standalone QueueCard without a group header or collapse arrow

#### Scenario: Series with multiple downloads

- **WHEN** a series has 2 or more items in the queue
- **THEN** it SHALL render as a QueueGroupCard with collapse header showing count

### Requirement: Expand All syncs to group cards

The Expand All / Collapse All toggle SHALL propagate to all QueueGroupCard instances, overriding their local expanded state.

#### Scenario: User clicks Expand All

- **WHEN** user clicks "Expand All"
- **THEN** all group cards SHALL expand regardless of their current state

#### Scenario: User clicks Collapse All

- **WHEN** user clicks "Collapse All"
- **THEN** all group cards SHALL collapse regardless of their current state
