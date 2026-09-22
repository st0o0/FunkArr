# Queue Context Menu

## Purpose

Context menu for queue items providing priority selection, force start, and delete actions.

## Requirements

### Requirement: Context menu trigger
Each queue item (active and queued) SHALL display a three-dot menu button that opens a context menu.

#### Scenario: Menu button visible
- **WHEN** a queue item is rendered
- **THEN** a menu button SHALL be visible on the right side of the card

### Requirement: Priority selection for queued items
The context menu for queued items SHALL show a priority radio group with High, Normal, and Low options. The current priority SHALL be visually marked.

#### Scenario: Change priority via menu
- **WHEN** a queued item's context menu is opened and "High" is selected
- **THEN** `POST /api/downloads/queue/{id}/priority` SHALL be called with `{ "priority": "High" }`
- **AND** the item SHALL move to the High bucket

#### Scenario: Current priority marked
- **WHEN** a Normal-priority item's context menu is opened
- **THEN** the "Normal" option SHALL have a check mark or active indicator

### Requirement: Force start action
The context menu for queued items SHALL include a "Force Start" action that dispatches the download immediately.

#### Scenario: Force start from menu
- **WHEN** "Force Start" is selected from a queued item's context menu
- **THEN** `POST /api/downloads/queue/{id}/force-start` SHALL be called
- **AND** the item SHALL move to the active section

### Requirement: Delete action
The context menu for all items (active and queued) SHALL include a "Delete" action.

#### Scenario: Delete from menu
- **WHEN** "Delete" is selected from a queue item's context menu
- **THEN** `DELETE /api/downloads/queue/{id}` SHALL be called
- **AND** the item SHALL be removed from the queue

### Requirement: Active download limited menu
The context menu for active (processing) downloads SHALL only show the Delete action. Priority and Force Start SHALL NOT be shown.

#### Scenario: Active item menu
- **WHEN** an active download's context menu is opened
- **THEN** only "Delete" SHALL be available
