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
