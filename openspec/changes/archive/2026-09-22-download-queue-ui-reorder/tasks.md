## 1. Backend: Atomic Move+Priority

- [x] 1.1 Extend `MoveDownload` command with `DownloadPriority? Priority = null` parameter
- [x] 1.2 Update `DownloadManager.HandleMove` to apply priority change before move when Priority is set (persist `DownloadPriorityChanged` + `DownloadMoved` atomically via `PersistAll`)
- [x] 1.3 Extend `MoveRequest` API model with optional `Priority` string field
- [x] 1.4 Update move endpoint to parse and pass optional priority to `MoveDownload`
- [x] 1.5 Add tests for combined move+priority operation
- [x] 1.6 Build and verify all existing tests pass

## 2. Frontend: API Client

- [x] 2.1 Add `priority` field to `QueueItem` TypeScript type
- [x] 2.2 Add `isPaused`, `isScheduleActive`, `nextWindow` fields to `QueueResponse` type
- [x] 2.3 Add `moveDownload(id, position, priority?)` API call
- [x] 2.4 Add `setDownloadPriority(id, priority)` API call
- [x] 2.5 Add `forceStartDownload(id)` API call
- [x] 2.6 Add `pauseDownloads()` and `resumeDownloads()` API calls

## 3. Frontend: Composables

- [x] 3.1 Update `useQueueStream.ts` to parse `priority`, `isPaused`, `isScheduleActive`, `nextWindow` from SSE events
- [x] 3.2 Create `usePriorityQueue.ts` — split queue items into `active`, `high`, `normal`, `low` computed arrays based on status and priority
- [x] 3.3 Create `useDragQueue.ts` — manage `isDragging` ref, SSE buffer, optimistic state snapshots, revert-on-failure logic

## 4. Frontend: Add vue-draggable-plus

- [x] 4.1 Install `vue-draggable-plus` via pnpm in `src/FunkArr.UI`

## 5. Frontend: Components

- [x] 5.1 Update `QueueCard.vue` — add drag handle grip icon on left, remove inline cancel button, add ⋮ menu trigger button
- [x] 5.2 Create `ActiveDownloadCard.vue` — progress bar, speed, ETA, channel/size badges, ⋮ button (delete only), no drag handle
- [x] 5.3 Create `PrioritySection.vue` — section header with priority label + count, draggable item list using `VueDraggable`, hidden when empty (visible as drop zone during drag)
- [x] 5.4 Create `QueueContextMenu.vue` — positioned dropdown: priority radio group (High/Normal/Low with checkmark), Force Start action, Delete action. Accept `isActive` prop to show delete-only for active items

## 6. Frontend: Activity View Rebuild

- [x] 6.1 Restructure `Activity.vue` from 3 tabs to 2 tabs (Queue + History)
- [x] 6.2 Build Queue tab layout: pipeline status bar at top, active section (non-draggable), then three `PrioritySection` components for High/Normal/Low
- [x] 6.3 Wire drag events: `onUpdate` for within-bucket moves (call `moveDownload`), `onAdd` for cross-bucket drops (call `moveDownload` with priority)
- [x] 6.4 Wire context menu: priority change calls `setDownloadPriority`, force start calls `forceStartDownload`, delete calls `deleteQueueItem`
- [x] 6.5 Add Pause/Resume button in queue header, bound to `pauseDownloads`/`resumeDownloads`
- [x] 6.6 Show pipeline state indicator (paused badge, schedule info with next window)
- [x] 6.7 Wire SSE buffering: set `isDragging` on drag start/end, composable handles buffer/flush
- [x] 6.8 Show empty-bucket drop zones during active drag, hide when drag ends

## 7. Frontend: Cleanup

- [x] 7.1 Remove `useGroupedQueue.ts` composable (replaced by `usePriorityQueue.ts`)
- [x] 7.2 Remove or repurpose `QueueGroupCard.vue` (replaced by `PrioritySection.vue`)
- [x] 7.3 Update `ActiveDownloads.vue` dashboard widget to use new `QueueItem` type with priority

## 8. i18n

- [x] 8.1 Add translation keys for priority labels (High/Normal/Low), pipeline states (Paused/Active/Scheduled), context menu actions (Force Start/Delete/Priority), section headers

## 9. Integration Test

- [x] 9.1 Start Docker dev, add downloads with different priorities via SABnzbd API
- [x] 9.2 Verify queue renders with correct priority sections and ordering
- [x] 9.3 Test drag-and-drop within bucket (verify move API called)
- [x] 9.4 Test drag-and-drop across buckets (verify move+priority API called)
- [x] 9.5 Test context menu: priority change, force start, delete
- [x] 9.6 Test pause/resume controls
- [x] 9.7 Verify SSE stream updates reflect changes without layout jumps
