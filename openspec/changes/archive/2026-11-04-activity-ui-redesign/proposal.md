## Why

The Activity page uses card-based layouts with drag-and-drop priority buckets and Queue/History tabs. With 200+ items in the queue (typical load from Sonarr/Radarr), this is unusable: scrolling through priority buckets of cards is impractical, drag-and-drop doesn't scale, and the tab split causes downloads to "vanish" when they complete. The Download Detail page compounds this by showing "Not Found" when a queue item finishes, because it brute-force scans 100 history items with `.find()` instead of fetching by ID. Since this is primarily a monitoring UI (downloads come from arr tools, priority changes are rare), the design should optimize for overview and status tracking, not manipulation.

## What Changes

- Replace Activity Queue/History tabs with a unified paginated table (25 items/page)
- Add filter chips with counts: All, Active, Queued, Completed, Failed
- Add search input that filters across all statuses (not just history)
- Show priority as a visible column (not hidden in context menus)
- Remove card-based queue layout, drag-and-drop reordering, priority bucket sections, and context menu
- Remove unused components: QueueGroupCard, PrioritySection, QueueContextMenu, ActiveDownloads
- Download Detail page: seamless transition from active to completed state instead of "Not Found"
- Add single-item history endpoint (`GET /api/download/history/{id}`) to replace brute-force lookup
- Detail page listens for SSE completion event to trigger live state transition

## Capabilities

### New Capabilities

- `activity-table`: Unified paginated table replacing tabs and card buckets, with filter chips, search, and status-aware columns
- `download-detail-transition`: Live state transition on the Download Detail page when a download completes, with single-item history endpoint

### Modified Capabilities

- `activity-view`: Replacing tab-based navigation with unified table and filters; search now applies across all statuses
- `download-queue-ui`: Removing priority bucket sections, drag-and-drop, QueueGroupCard; queue layout requirement replaced by activity-table

## Impact

- **Frontend**: Activity.vue rewritten, DownloadDetail.vue updated for live transitions, several components removed (QueueGroupCard, PrioritySection, QueueContextMenu, ActiveDownloads, ActiveDownloadCard)
- **Backend API**: New endpoint `GET /api/download/history/{id}` in FunkArr.Api
- **SSE**: Existing queue stream used; detail page needs to detect item completion from stream events
- **Routing**: `/activity` route preserved, `/queue` and `/history` redirects preserved
- **Dashboard**: Not affected (separate concern)
