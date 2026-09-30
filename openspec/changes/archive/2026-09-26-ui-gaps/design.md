## Approach

Three new views following existing patterns: plain `fetch()` API, Vue composables, Tailwind utility classes, vue-i18n translations.

## Download Detail View

Route: `/activity/:id` -- new view `DownloadDetail.vue`

Data sources:
- Queue item: fetched from `useQueueStream()` composable, filtered by `downloadId` in the SSE stream
- History item: fetched from `GET /api/downloads/history` when not found in queue (completed/failed downloads)

Sections:
- Header: title, channel badge, quality, subtitle badge, status
- Progress: bar + percentage + bytes + speed + ETA (live from SSE, only for active)
- Details: file path, download time, error message (for history items)
- Actions: context menu actions as buttons (force-start, delete, retry depending on state)

Navigation: `ActiveDownloadCard` and history table rows in Activity.vue become clickable via `router-link` or `@click` + `router.push`.

No new API endpoints needed. All data available from existing queue SSE + history endpoint.

## Settings View

Route: `/settings` -- new view `Settings.vue`

New sidebar nav entry with gear icon (between RuleSets and Setup footer section).

Sections, each fetched in parallel:
- **Downloads**: concurrent slots, schedule windows (from `GET /api/downloads/settings`)
- **Metadata Cache**: TVDB/TMDB entries, oldest entry (from `GET /api/system/cache`)
- **Network Routes**: route definitions + channel mappings (from new `GET /api/system/routes`)
- **System**: versions, database, data path, FFmpeg, API key masked (from `GET /api/system/version` + `GET /api/system/setup`)

New API endpoint: `GET /api/system/routes` in `SystemApiEndpoints` returning the `RoutingOptions` as read-only JSON.

New API client: `getDownloadSettings()` in `downloads.ts`, `getRoutes()` in `setup.ts`.

## Log Viewer

Lives as a tab or section within the Settings view (not a separate route).

Backend:
- `RingBufferSink` -- Serilog `ILogEventSink` that stores last 500 entries in a `ConcurrentQueue<LogEntry>`. Registered in `LoggingSetupContainer`.
- `LogEntry` record: timestamp, level, message, sourceContext, exception
- `GET /api/system/logs` -- returns the current ring buffer contents
- `GET /api/system/logs/stream` -- SSE endpoint pushing new entries. The sink notifies a channel, the endpoint reads from it.

Frontend:
- `useLogStream()` composable (same SSE pattern as `useQueueStream`)
- Log list with level-colored badges, timestamp, source context, message
- Level filter buttons (All / Info / Warning / Error)
- Auto-scroll toggle

## i18n

New keys under `settings.*` and `nav.settings` in all 4 locale files.
New keys under `downloadDetail.*` for the detail view.
