## 1. Backend: Single-Item History Endpoint

- [x] 1.1 Add `QuerySingleHistoryItem` message to DownloadHistoryManager (query + response types in Messages)
- [x] 1.2 Handle `QuerySingleHistoryItem` in DownloadHistoryManager actor — lookup by download ID, return item or not-found response
- [x] 1.3 Add `GET /api/download/history/{id}` endpoint in FunkArr.Api DownloadApi, wired to the new message
- [x] 1.4 Add test for single-item history endpoint (fetch existing, fetch missing → 404)

## 2. Frontend: Clean Up SSE Composable

- [x] 2.1 Remove drag-buffering logic from `useQueueStream` (`isDragging`, `flushBuffer`, related refs)

## 3. Frontend: Activity Table View

- [x] 3.1 Create `ActivityTable.vue` component — paginated table with columns (Position, Title, Status, Priority, Size, Actions)
- [x] 3.2 Create filter chips component with counts (All, Active, Queued, Completed, Failed), wired to SSE counts + history total
- [x] 3.3 Add search input with debounced filtering across queue (client-side) and history (server-side)
- [x] 3.4 Add Priority and Channel filter dropdowns
- [x] 3.5 Implement hybrid data merging — SSE items for active/queued, REST pagination for history, combined for "All" filter
- [x] 3.6 Add inline row actions (Delete for all states, Retry for failed) with toast feedback
- [x] 3.7 Add pagination controls with page navigation and "Showing X-Y of Z" display

## 4. Frontend: Rewrite Activity View

- [x] 4.1 Rewrite `Activity.vue` — replace tabs with search + filter chips + ActivityTable, keep SSE composable at view level
- [x] 4.2 Update router: remove tab-specific redirect logic, keep `/queue` and `/history` redirecting to `/activity`

## 5. Frontend: Download Detail Live Transition

- [x] 5.1 Add `getHistoryItem(id)` API call to fetch single history item via `GET /api/download/history/{id}`
- [x] 5.2 Rewrite `DownloadDetail.vue` item resolution — watch SSE for queue item, on disappearance fetch history item by ID with retry
- [x] 5.3 Show phase stepper in completed/failed state (all phases done for completed, failed phase highlighted for failed)
- [x] 5.4 Add transition banner — success toast on completion, error toast on failure (only when user was watching the active state)

## 6. Frontend: Remove Unused Components

- [x] 6.1 Remove `QueueGroupCard.vue`, `PrioritySection.vue`, `QueueContextMenu.vue`, `ActiveDownloads.vue`, `ActiveDownloadCard.vue`
- [x] 6.2 Remove `vue-draggable-plus` dependency if no other component uses it
- [x] 6.3 Remove `QueueCard.vue` if fully replaced by table rows (verify no other usage)

## 7. Verify

- [x] 7.1 Build and run dev stack, verify Activity page with queue items and history pagination
- [x] 7.2 Verify Download Detail page transition — watch an active download complete, confirm no "Not Found"
- [ ] 7.3 Verify filter chips, search, and pagination work correctly with 200+ items (needs 200+ items)
