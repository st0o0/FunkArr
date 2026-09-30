# Download Queue UI (Delta)

## ADDED Requirements

### Requirement: Priority display in queue
The download queue view SHALL display the priority of each queued item.

#### Scenario: Priority badge
- **WHEN** a download has priority High or Low
- **THEN** the queue item SHALL display a visual priority indicator
- **AND** Normal-priority items SHALL NOT display a priority indicator (it is the default)

### Requirement: Priority grouping
The download queue view SHALL visually group items by priority bucket.

#### Scenario: Visual separation
- **WHEN** the queue contains items of different priorities
- **THEN** items SHALL be displayed in priority order (High, Normal, Low) matching the backend queue order
