## Context

The Activity page currently uses a card-based layout with three priority buckets (High, Normal, Low), drag-and-drop reordering, and a context menu for priority changes. Queue and History are separate tabs. With 200+ items (typical Sonarr/Radarr load), this is unusable. Downloads are added by arr tools, not manually — the UI is primarily monitoring.

The Download Detail page (`/activity/:id`) shows live progress for active downloads but breaks when a download completes: the queue item disappears from SSE, and the page falls back to a brute-force scan of 100 history items via `getHistory(0, 100).find()`. If the item isn't in the first 100, the page shows "Not Found."

Current SSE composable (`useQueueStream`) exposes reactive queue state from `/api/downloads/queue/stream`. It includes drag-buffering logic (`isDragging`, `flushBuffer`) that will no longer be needed.

## Goals / Non-Goals

**Goals:**

- Scalable monitoring view for 200+ downloads via paginated table with filters
- Unified view across all download states (active, queued, completed, failed)
- Seamless Detail page transition when a download completes
- Dedicated single-item history endpoint to replace brute-force lookup

**Non-Goals:**

- Dashboard redesign (separate concern, not touched)
- Bulk operations (monitoring-first, downloads managed by arr tools)
- New priority management workflows (priority column visible but rarely used)
- Backend changes to priority/reorder commands (endpoints stay, UI drag removed)
- Changes to SSE data shape or queue stream endpoint

## Decisions

### 1. Unified table instead of tabs + cards

Replace Queue/History tabs with a single paginated table showing all download states. Filter chips (All, Active, Queued, Completed, Failed) replace tabs. The table uses server-side pagination for history items and client-side SSE data for active/queued items.

**Why over keeping tabs:** Tabs cause the "vanishing download" problem. A unified view means a completing download simply changes its status indicator in-place rather than moving to another tab. With 200+ items, a table with pagination is the only viable layout.

**Why over virtual scrolling:** Pagination is simpler, works with server-side history data, and matches the monitoring use case (scan pages, not scroll endlessly).

### 2. Hybrid data strategy: SSE for queue, REST for history

The table merges two data sources:
- **Active/Queued items**: From `useQueueStream` SSE (real-time, already exists)
- **Completed/Failed items**: From `GET /api/downloads/history` REST endpoint (paginated)

When filter is "All", active+queued items appear first (from SSE), followed by paginated history. When filter is "Active" or "Queued", only SSE data is shown (no pagination needed). When filter is "Completed" or "Failed", only history REST data is shown (paginated).

**Why not unify into one endpoint:** The SSE stream already provides excellent real-time queue data. Adding history to it would mean streaming 200+ completed items on every update. The hybrid approach keeps the fast SSE path for live data and uses REST pagination for the large history set.

### 3. Single-item history endpoint for Detail page

Add `GET /api/download/history/{id}` that queries DownloadHistoryManager for one item by ID. The Detail page uses this as fallback when the queue item disappears from SSE, instead of scanning 100 items client-side.

**Why a new endpoint over fixing the client scan:** The client scan is fundamentally broken — it can only find items in the first N results. A dedicated endpoint is O(1) and correct.

### 4. Detail page SSE-driven transition

When the Detail page is watching an active download and the item disappears from the SSE queue stream, the page:
1. Detects the item is no longer in `useQueueStream.items`
2. Fetches `GET /api/download/history/{id}` to get the completed/failed record
3. Transitions the UI to show completion state (phase stepper all done, metadata from history)

No new SSE event type needed. The existing queue stream already drops completed items — the Detail page just needs to react to the absence instead of showing "Not Found."

**Why not a dedicated "completed" SSE event:** The queue stream already signals completion implicitly (item disappears). Adding a new event type would require backend changes to DownloadManager's SSE emission logic. Detecting absence + fetching history is simpler and requires only the new REST endpoint.

### 5. Remove drag-and-drop and context menu components

Remove QueueGroupCard, PrioritySection, QueueContextMenu, ActiveDownloads, and ActiveDownloadCard components. Remove drag-buffering logic from `useQueueStream` (`isDragging`, `flushBuffer`). The `vue-draggable-plus` dependency can be removed if nothing else uses it.

Priority is visible as a table column. For the rare case of changing priority, the existing `POST /api/downloads/queue/{id}/priority` endpoint stays — a future inline action can call it. The Detail page already has priority display.

**Why not keep context menu for actions:** The table row itself will have inline action buttons (delete, retry for failed). A context menu adds hidden complexity for rarely-used features. Keep it simple.

## Risks / Trade-offs

**[Mixed data sources in table]** Merging SSE queue data and REST history data in one table adds complexity to sorting and pagination. → Mitigation: Active/queued items always appear first (they're live and few compared to history). Pagination only applies to history items. The filter chips make it clear which data source is active.

**[History fetch on every filter change]** Switching to "Completed" or "All" triggers a REST call. → Mitigation: This is the existing behavior from the History tab. Cache the last result and refetch only on filter/page change.

**[Detail page race condition]** Item disappears from SSE before history endpoint returns it (history write might lag). → Mitigation: Retry the history fetch after a short delay (1-2s) if the first attempt returns 404. The window is small since history is written synchronously in the Persist callback.

**[Component removal]** Removing 5 components is a larger diff. → Mitigation: The components are self-contained with no cross-references beyond Activity.vue. Clean removal in one task.
