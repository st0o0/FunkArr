# Queue Drag Reorder

## Purpose

Drag-and-drop queue reordering UI with cross-bucket priority changes, optimistic updates, and SSE buffering.

## Requirements

### Requirement: Drag handle on queued items
Each queued download card SHALL display a drag handle (grip icon) on the left side. Active downloads SHALL NOT have a drag handle.

#### Scenario: Drag handle visible
- **WHEN** a download is in Queued status
- **THEN** a drag handle icon SHALL be visible on the left of the card

#### Scenario: No drag handle on active
- **WHEN** a download is in Processing status
- **THEN** no drag handle SHALL be displayed

### Requirement: Within-bucket reorder
The user SHALL be able to drag a queued item to a new position within the same priority bucket. The UI SHALL call `POST /api/downloads/queue/{id}/move` with the target position.

#### Scenario: Drag item down within Normal bucket
- **WHEN** a Normal-priority item at position 2 is dragged to position 4
- **THEN** the API SHALL be called with `{ "position": 4 }`
- **AND** the item SHALL appear at position 4 immediately (optimistic)

### Requirement: Cross-bucket reorder
The user SHALL be able to drag a queued item into a different priority bucket. The UI SHALL call `POST /api/downloads/queue/{id}/move` with both `position` and `priority`.

#### Scenario: Drag Normal item into High bucket
- **WHEN** a Normal-priority item is dragged into the High priority section
- **THEN** the API SHALL be called with `{ "position": <target>, "priority": "High" }`
- **AND** the item SHALL appear in the High section immediately (optimistic)

### Requirement: Empty bucket drop zones
Empty priority buckets SHALL be hidden by default. During an active drag, empty buckets SHALL appear as drop zones.

#### Scenario: Empty High bucket during drag
- **WHEN** no High-priority items exist and a drag starts
- **THEN** the High section SHALL appear as a drop target
- **WHEN** the drag ends without dropping in High
- **THEN** the High section SHALL hide again

### Requirement: SSE buffering during drag
The queue SSE stream SHALL buffer incoming updates while a drag is active. Buffered state SHALL be applied after the drop completes.

#### Scenario: SSE update during drag
- **WHEN** an SSE queue update arrives while the user is dragging
- **THEN** the update SHALL be buffered, not applied to the UI
- **WHEN** the drag ends
- **THEN** the latest buffered state SHALL be reconciled with the optimistic result

### Requirement: Optimistic UI with revert
Move and priority operations SHALL update the UI immediately before the API response. On failure, the UI SHALL revert to the pre-operation state.

#### Scenario: API failure reverts UI
- **WHEN** a drag-and-drop move is performed and the API returns an error
- **THEN** the item SHALL revert to its original position and priority
