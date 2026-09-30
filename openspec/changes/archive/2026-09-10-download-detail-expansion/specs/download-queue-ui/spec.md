## MODIFIED Requirements

### Requirement: Queue page displays active downloads as cards
The Queue page SHALL display each active (Processing) download as a card with Level 2 card styling (hover lift effect). Cards SHALL show title, channel, category, size, a progress bar, percentage, download speed, ETA, subtitle indicator, and video duration. Cards SHALL have `hover:-translate-y-px hover:shadow-md transition-all` for interactive feel.

#### Scenario: Active download card
- **WHEN** a download has status "Processing"
- **THEN** the card SHALL display the title as heading, channel and category as metadata, total size formatted in MB/GB, a visual progress bar filled to the current percentage, the percentage as text, speed formatted as MB/s, ETA as HH:MM:SS, video duration formatted (e.g., "52 min"), and a subtitle indicator if subtitles are included

#### Scenario: Subtitle indicator present
- **WHEN** a download has `hasSubtitles` true
- **THEN** the card SHALL display a "Sub" badge or indicator alongside the metadata

#### Scenario: Subtitle indicator absent
- **WHEN** a download has `hasSubtitles` false
- **THEN** the card SHALL NOT display a subtitle indicator

#### Scenario: Duration display
- **WHEN** a download has `totalDuration` of 3120 seconds
- **THEN** the card SHALL display "52 min" as the video duration

#### Scenario: Progress bar visualization
- **WHEN** a download is at 72% progress
- **THEN** the progress bar fill width SHALL be 72% of the bar container
- **AND** the bar SHALL use `brand-500` color for the filled portion

#### Scenario: Card hover lift
- **WHEN** the user hovers over a download card
- **THEN** the card SHALL translate up by 1px and show a subtle shadow

### Requirement: Queue page displays queued items as cards
The Queue page SHALL display each queued (waiting) download as a simpler card showing title, channel, category, size, and video duration without progress data.

#### Scenario: Queued item card
- **WHEN** a download has status "Queued"
- **THEN** the card SHALL display title, channel, category, size, video duration, and subtitle indicator
- **AND** SHALL NOT display a progress bar, speed, or ETA
