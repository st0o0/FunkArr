# Download Queue UI (Delta)

## MODIFIED Requirements

### Requirement: Queue view layout
The download activity view SHALL display two tabs: Queue and History. The Queue tab SHALL show active downloads in a pinned non-draggable section at the top, followed by priority-bucketed queue sections (High, Normal, Low) below.

#### Scenario: Merged queue view
- **WHEN** the user navigates to the Activity/Queue view
- **THEN** active downloads SHALL be shown at the top
- **AND** queued downloads SHALL be grouped by priority below

#### Scenario: Empty queue
- **WHEN** no downloads are active or queued
- **THEN** an empty state message SHALL be displayed

### Requirement: Priority bucket sections
Each priority level (High, Normal, Low) SHALL have a labeled section header showing the priority name and item count. Sections with no items SHALL be hidden (except during drag).

#### Scenario: Priority section header
- **WHEN** the Normal bucket contains 3 items
- **THEN** the section header SHALL display "Normal (3)"

#### Scenario: Empty bucket hidden
- **WHEN** the High bucket contains no items and no drag is active
- **THEN** the High section SHALL not be rendered

## ADDED Requirements

### Requirement: Priority badge on queue cards
Queue cards SHALL NOT display a priority badge — the section header provides the priority context. This avoids visual redundancy.

#### Scenario: No badge on Normal item
- **WHEN** a Normal-priority item is rendered inside the Normal section
- **THEN** no priority badge or label SHALL appear on the card itself
