## MODIFIED Requirements

### Requirement: Dashboard active downloads widget
The Dashboard page SHALL include a compact "Active Downloads" widget showing active downloads with progress bars and a link to the Queue page.

#### Scenario: Widget with active downloads
- **WHEN** there are active downloads in the SSE stream
- **THEN** the widget SHALL display each active download with title, percentage, speed, and a compact progress bar

#### Scenario: Widget with queued count
- **WHEN** there are queued items in the SSE stream
- **THEN** the widget SHALL display a count of queued items below the active downloads

#### Scenario: Widget empty state
- **WHEN** the queue is completely empty
- **THEN** the widget SHALL display "No active downloads"

#### Scenario: View queue link
- **WHEN** the widget renders
- **THEN** it SHALL include a "View Queue" link navigating to `/queue`

#### Scenario: Dashboard stat cards show queue split
- **WHEN** the SSE stream includes `activeCount` and `queuedCount`
- **THEN** the Dashboard SHALL display separate stat cards for "Active" (showing active/totalSlots) and "Queued" (showing queued count)

### Requirement: Queue page summary footer
The Queue page SHALL display a summary showing total item count, queued count, and active count.

#### Scenario: Summary with items
- **WHEN** the queue has 3 items (1 active, 2 queued)
- **THEN** the summary SHALL display "3 items - 2 queued - 1 downloading"

#### Scenario: Summary uses response counts
- **WHEN** the SSE stream includes `activeCount` and `queuedCount`
- **THEN** the summary SHALL use these values directly instead of computing them client-side
