# Download Queue UI

## Purpose

Global SSE composable for reactive download queue state, dashboard active downloads widget, and shared formatting utilities for size and speed display.
## Requirements
### Requirement: Global SSE composable
The application SHALL provide a `useQueueStream` composable that connects to the SSE endpoint and exposes reactive queue state. This composable is unchanged and remains shared.

#### Scenario: SSE connection on Activity mount
- **WHEN** the Activity view mounts
- **THEN** `useQueueStream` SHALL open an EventSource connection to `/api/downloads/queue/stream`

#### Scenario: Reactive state updates
- **WHEN** the SSE stream receives a `queue` event
- **THEN** the composable SHALL parse the JSON data and update its reactive `items` ref

#### Scenario: Composable shared state
- **WHEN** multiple components within the Activity view call `useQueueStream`
- **THEN** they SHALL share the same EventSource connection and reactive state

### Requirement: Dashboard active downloads widget
The Overview page (formerly Dashboard) SHALL include a compact "Active Downloads" section showing active downloads with progress bars. The widget SHALL link to the Activity view instead of the Queue page.

#### Scenario: View all link target
- **WHEN** the active downloads widget renders
- **THEN** it SHALL include a "View All" link navigating to `/activity`

#### Scenario: Widget with active downloads
- **WHEN** there are active downloads in the SSE stream
- **THEN** the widget SHALL display each active download with title, percentage, speed, and a compact progress bar

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

### Requirement: Priority badge on queue cards
Queue cards SHALL NOT display a priority badge — the section header provides the priority context. This avoids visual redundancy.

#### Scenario: No badge on Normal item
- **WHEN** a Normal-priority item is rendered inside the Normal section
- **THEN** no priority badge or label SHALL appear on the card itself

### Requirement: Active download phase label
The ActiveDownloadCard SHALL display the current download phase alongside the progress percentage. During the "downloading" phase, the label SHALL read the localized equivalent of "Downloading". During the "remuxing" phase, the label SHALL read the localized equivalent of "Remuxing".

#### Scenario: Downloading phase shows download label
- **WHEN** a queue item has `phase = "downloading"`
- **THEN** the progress line SHALL show "Downloading" (or localized equivalent) before the percentage

#### Scenario: Remuxing phase shows remux label
- **WHEN** a queue item has `phase = "remuxing"`
- **THEN** the progress line SHALL show "Remuxing" (or localized equivalent) before the percentage

