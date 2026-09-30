## MODIFIED Requirements

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

### Requirement: Queue page loading state

The Queue page SHALL display loading skeletons while the initial SSE connection is being established and no data has been received yet. Skeletons SHALL mimic the shape of download cards.

#### Scenario: Initial loading
- **WHEN** the Queue page mounts and no SSE data has arrived yet
- **THEN** skeleton card placeholders with shimmer animation SHALL be displayed

#### Scenario: Data arrives
- **WHEN** the first SSE event is received
- **THEN** skeletons SHALL be replaced with actual download cards or the empty state
