## Why

FunkArr's UI has 8 views but three notable gaps: no way to see download details beyond the queue card, no settings/config overview, and no log visibility. Several backend API endpoints exist with no UI consumer (`/api/downloads/settings`, `/api/system/cache`, `/api/downloads/history/stats`).

## What Changes

Three new views:

### 1. Download Detail View (`/activity/:id`)

A drill-down from the Activity queue/history cards showing full download information:
- Status, phase, channel, quality, subtitle format
- Live progress (from existing SSE stream, filtered client-side)
- File paths (incomplete + complete)
- Error details for failed downloads
- Action buttons (pause, force-start, delete, retry)

Works for both active queue items and completed history items. The SSE stream already pushes live updates -- the view filters by download ID client-side.

No new API endpoints needed. `QueueItem` and `HistoryItem` already carry all the data. The view reads from the existing queue/history endpoints and SSE stream.

### 2. Settings Page (`/settings`)

Read-only config dashboard from existing API endpoints. New sidebar navigation entry.

Sections:
- **Downloads**: concurrent downloads, schedule (from `GET /api/downloads/settings`)
- **Metadata Cache**: TVDB/TMDB entry counts, oldest entry (from `GET /api/system/cache`)
- **Network Routes**: route definitions, channel mappings (needs new read-only endpoint `GET /api/system/routes`)
- **System**: version, ruleset version, database type, data path, FFmpeg version, API key masked (from `GET /api/system/version` + `GET /api/system/setup`)

Phase 1 is purely read-only. Write actions (clear cache, change concurrent downloads) deferred to a future change.

### 3. Log Viewer (`/settings/logs` or tab within Settings)

Lightweight real-time log viewer using a Serilog ring-buffer sink:
- New `RingBufferSink` that holds the last N log entries in memory (configurable, default 500)
- New SSE endpoint `GET /api/system/logs/stream` that pushes new log entries
- New REST endpoint `GET /api/system/logs` that returns the current buffer (for initial page load)
- UI shows a scrollable log list with level filtering (Info/Warning/Error), auto-scroll, and source context

## Capabilities

### New Capabilities

- `download-detail-view`: Vue route + view for `/activity/:id` with live SSE progress
- `settings-view`: Vue route + view for `/settings` with config sections
- `routes-endpoint`: `GET /api/system/routes` returning current route definitions and channel mappings
- `log-ring-buffer`: Serilog `RingBufferSink` holding last N entries in memory
- `log-endpoints`: `GET /api/system/logs` + `GET /api/system/logs/stream` (SSE)
- `log-viewer`: Vue component within settings for real-time log display

### Modified Capabilities

- `sidebar-layout`: New Settings entry in sidebar navigation
- `activity-view`: Queue/history cards become clickable, navigating to detail view
- `logging-setup`: Adds RingBufferSink alongside existing console sink

## Impact

- **FunkArr.UI**: 3 new views, updated router, updated sidebar, new API modules, new composables
- **FunkArr.Api**: New routes endpoint, new log endpoints
- **FunkArr (Host)**: RingBufferSink registration in LoggingSetupContainer
- No actor changes, no message changes
