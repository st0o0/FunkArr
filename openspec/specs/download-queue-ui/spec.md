# Download Queue UI

## Purpose

Queue page showing live download status with card layout, progress visualization, and cancel/delete actions. Includes a global SSE composable and a compact dashboard widget.

## Requirements

### Requirement: Queue page route
The application SHALL register a `/queue` route rendering the Queue view.

#### Scenario: Navigation to queue page
- **WHEN** the user navigates to `/queue`
- **THEN** the Queue view SHALL render within the AppLayout

### Requirement: Queue page displays active downloads as cards
The Queue page SHALL display each active (Processing) download as a card with Level 2 card styling (hover lift effect). Cards SHALL show title, channel, category, size, a progress bar, percentage, download speed, and ETA. Cards SHALL have `hover:-translate-y-px hover:shadow-md transition-all` for interactive feel.

#### Scenario: Active download card
- **WHEN** a download has status "Processing"
- **THEN** the card SHALL display the title as heading, channel and category as metadata, total size formatted in MB/GB, a visual progress bar filled to the current percentage, the percentage as text, speed formatted as MB/s, and ETA as HH:MM:SS

#### Scenario: Progress bar visualization
- **WHEN** a download is at 72% progress
- **THEN** the progress bar fill width SHALL be 72% of the bar container
- **AND** the bar SHALL use `brand-500` color for the filled portion

#### Scenario: Card hover lift
- **WHEN** the user hovers over a download card
- **THEN** the card SHALL translate up by 1px and show a subtle shadow

### Requirement: Queue page displays queued items as cards
The Queue page SHALL display each queued (waiting) download as a simpler card showing title, channel, category, and size without progress data.

#### Scenario: Queued item card
- **WHEN** a download has status "Queued"
- **THEN** the card SHALL display title, channel, category, and size
- **AND** SHALL NOT display a progress bar, speed, or ETA

### Requirement: Queue page shows empty state
The Queue page SHALL display a structured empty state when no downloads are in the queue. The empty state SHALL include a download icon (from the existing SVG icon set), a title text ("No active downloads"), and a descriptive subtitle explaining what triggers downloads.

#### Scenario: Empty queue
- **WHEN** there are no queued or active downloads
- **THEN** the page SHALL display a centered empty state with icon, title "No active downloads", and subtitle "Downloads appear here when Sonarr or Radarr trigger a search."

### Requirement: Queue page cancel/delete action
Each queue item card SHALL have a delete/cancel action button. On successful cancellation, a toast notification SHALL be shown.

#### Scenario: Cancel active download
- **WHEN** the user clicks the cancel button on an active download card
- **THEN** the system SHALL send `DELETE /api/downloads/queue/{id}`
- **AND** a success toast SHALL display "Download cancelled"

#### Scenario: Cancel error
- **WHEN** the cancel API call fails
- **THEN** an error toast SHALL display the error message

#### Scenario: Delete queued item
- **WHEN** the user clicks the delete button on a queued item card
- **THEN** the system SHALL send `DELETE /api/downloads/queue/{id}`

### Requirement: Queue page loading state
The Queue page SHALL display loading skeletons while the initial SSE connection is being established and no data has been received yet. Skeletons SHALL mimic the shape of download cards.

#### Scenario: Initial loading
- **WHEN** the Queue page mounts and no SSE data has arrived yet
- **THEN** skeleton card placeholders with shimmer animation SHALL be displayed

#### Scenario: Data arrives
- **WHEN** the first SSE event is received
- **THEN** skeletons SHALL be replaced with actual download cards or the empty state

### Requirement: Queue page summary footer
The Queue page SHALL display a summary showing total item count, queued count, and active count.

#### Scenario: Summary with items
- **WHEN** the queue has 3 items (1 active, 2 queued)
- **THEN** the summary SHALL display "3 items · 2 queued · 1 downloading"

### Requirement: Global SSE composable
The application SHALL provide a `useQueueStream` composable that connects to the SSE endpoint and exposes reactive queue state.

#### Scenario: SSE connection on app mount
- **WHEN** the application mounts
- **THEN** `useQueueStream` SHALL open an EventSource connection to `/api/downloads/queue/stream`

#### Scenario: Reactive state updates
- **WHEN** the SSE stream receives a `queue` event
- **THEN** the composable SHALL parse the JSON data and update its reactive `items` ref

#### Scenario: Auto-reconnect
- **WHEN** the SSE connection drops
- **THEN** EventSource SHALL automatically reconnect (native behavior)

#### Scenario: Composable shared state
- **WHEN** multiple components call `useQueueStream`
- **THEN** they SHALL share the same EventSource connection and reactive state

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

### Requirement: Size formatting
All size values SHALL be formatted in human-readable units (bytes → KB/MB/GB) with one decimal place.

#### Scenario: Megabyte formatting
- **WHEN** a size value is 245000000 bytes
- **THEN** it SHALL display as "233.6 MB"

#### Scenario: Gigabyte formatting
- **WHEN** a size value is 1200000000 bytes
- **THEN** it SHALL display as "1.1 GB"

### Requirement: Speed formatting
Download speed SHALL be formatted in human-readable units per second.

#### Scenario: Speed in MB/s
- **WHEN** speed is 12900000 bytes/second
- **THEN** it SHALL display as "12.3 MB/s"
