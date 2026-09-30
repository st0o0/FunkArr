## Context

The FunkArr UI currently has Dashboard, RuleSets, and Setup pages. The download pipeline (DownloadManager, DownloadWorker, DownloadHistoryActor) is fully implemented with actors and messages, and the SABnzbd-compat adapter (`/download/api`) translates these for Sonarr/Radarr. But the internal UI has no download visibility.

All required messages already exist: `QueryQueue`, `QueryHistory`, `DeleteDownload`, `RetryDownload`, `RemoveHistoryEntry`. The actors are registered in `IActorRegistry` as `IDownloadManager` and `IDownloadHistory`. No backend actor changes are needed.

The UI is Vue 3 + Vite + Tailwind v4 with design tokens, vue-router, and a sidebar layout. API clients live in `src/api/*.ts`.

## Goals / Non-Goals

**Goals:**
- Internal REST API for the UI to access download queue and history
- SSE stream for real-time queue updates without polling from the client
- Queue page with live progress visualization
- History page with pagination and actions
- Dashboard widget showing active downloads
- Two new sidebar navigation items

**Non-Goals:**
- WebSocket or SignalR — SSE is sufficient for server-to-client push
- Actor-level push (EventStream subscription) — API-polls-actor is simpler and adequate
- Drag-to-reorder queue priority — queue is FIFO, no reordering
- Download speed graphs or historical stats
- Search/filter on queue page (too few items to matter)

## Decisions

### 1. Internal API in FunkArr.Api, not ArrApi

The SABnzbd adapter in `FunkArr.ArrApi` uses `?mode=` query parameter dispatch and SABnzbd wire format. The internal API uses clean REST routes and JSON response models. Placing it in `FunkArr.Api` (alongside `RuleSetApiEndpoints` and `SetupApiEndpoints`) follows the existing pattern.

Endpoints:
- `GET /api/downloads/queue` — one-shot queue snapshot
- `GET /api/downloads/queue/stream` — SSE stream (text/event-stream)
- `GET /api/downloads/history?start=0&limit=25&category=` — paginated history
- `DELETE /api/downloads/queue/{id:guid}` — cancel queued/active download
- `DELETE /api/downloads/history/{id:guid}` — remove history entry
- `POST /api/downloads/{id:guid}/retry` — retry failed download

### 2. SSE via API-polls-actor pattern

The SSE endpoint opens a long-lived HTTP response. A server-side loop queries `DownloadManager` via `QueryQueue` every 3 seconds and writes JSON events. When the client disconnects, the `CancellationToken` fires and the loop exits.

Why not actor-push: Would require a bridge actor per SSE connection to receive EventStream events and marshal to the HTTP response. The queue has at most ~10 items and polling cost is negligible. Can upgrade later without changing the SSE contract.

Why not client-side polling: SSE gives instant updates when the server has them, single persistent connection vs repeated HTTP round-trips, and automatic reconnection built into EventSource.

### 3. Global Vue composable for SSE

A `useQueueStream()` composable using native `EventSource` connects once at the app level (via `App.vue` or a provider). It exposes reactive `ref<QueueItem[]>` state consumed by both Queue.vue and the dashboard ActiveDownloads widget.

The composable handles:
- Connection lifecycle (connect on mount, close on unmount of last consumer)
- Auto-reconnect (EventSource does this natively)
- JSON parsing of SSE data field
- Reactive state updates

### 4. Queue page: card layout

Active/processing downloads render as cards with: title, channel, category, size, progress bar, percentage, speed, ETA. Queued items render as simpler cards without progress. Each card has a delete/cancel action button.

### 5. History page: table layout

Table with columns: Title, Channel, Category, Size, Duration, Status, Completed. Failed items show a retry button. Each row has a delete action. Pagination controls at the bottom using `start`/`limit` query params.

### 6. API response models

New records in `FunkArr.Api/Models/` that map from the actor messages (`QueueResult`, `HistoryResult`) to clean JSON DTOs. These decouple the API contract from internal message shapes. Computed fields like `percentage`, `speed` (formatted), and `eta` are calculated in the API layer.

### 7. Navigation order

Sidebar items become: Dashboard, Queue, History, RuleSets, Setup. Download-related items sit together, above the configuration-oriented items.

## Risks / Trade-offs

- **SSE connection limit**: Browsers limit ~6 concurrent SSE connections per domain. Since FunkArr runs as a single-tab tool, this is not a practical concern. If it ever becomes one, upgrading to WebSocket is straightforward.
- **3-second poll interval**: Active downloads update position ~once per second (FFmpeg progress). A 3-second server-side poll means the UI can lag up to 3 seconds behind real progress. Acceptable for a media download tool.
- **No authentication on SSE**: The internal API (`/api/`) currently has no auth (it's behind the network boundary). SSE follows the same model. If auth is added later, SSE needs cookie/token support.
- **History pagination state**: Page state (current page, filters) lives in Vue component state and URL query params. Navigating away and back resets to page 1 unless we persist in the URL, which we do.
