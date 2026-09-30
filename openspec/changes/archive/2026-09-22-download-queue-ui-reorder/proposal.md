## Why

The download queue UI was built before priority support existed. It shows a flat list with no way to see or change priorities, no drag-and-drop reordering, and separates active/queued downloads into different tabs. With the priority and reorder backend now in place (`download-queue-reorder` change), the UI needs a complete rebuild to expose these capabilities.

## What Changes

- **BREAKING**: Merge "Aktiv" and "Wartend" tabs into one "Queue" tab — active downloads pinned at top, priority-bucketed queue below
- Remove the third "Aktiv" tab (2 tabs remain: Queue + Verlauf)
- Add drag-and-drop reordering with `vue-draggable-plus` (SortableJS)
- Cross-bucket drag automatically changes priority via atomic move+priority API call
- Add priority bucket sections (High/Normal/Low) with counts, hidden when empty
- Add context menu (three-dot) on queue items: priority radio, force start, delete
- Add pause/resume button in queue header showing pipeline state
- Buffer SSE updates during active drag to prevent layout jumps
- Extend `MoveDownload` command/endpoint with optional `Priority` parameter for atomic cross-bucket moves
- Add missing API client calls: move, priority, force-start, pause, resume
- Update `QueueItem` and `QueueResponse` TypeScript types to include priority and pipeline state fields

## Capabilities

### New Capabilities

- `queue-drag-reorder`: Drag-and-drop queue reordering UI with cross-bucket priority changes and SSE buffering
- `queue-context-menu`: Context menu for queue items with priority selection, force start, and delete actions
- `queue-pipeline-controls`: Pause/resume controls and pipeline state display in queue header

### Modified Capabilities

- `download-queue-ui`: Complete rebuild — merge tabs, add priority sections, replace series grouping with priority buckets
- `download-queue-reorder`: Extend MoveDownload with optional Priority for atomic cross-bucket moves
- `download-messages`: MoveDownload gains optional Priority parameter; MoveRequest API model extended

## Impact

- **FunkArr.Messages**: `MoveDownload` extended with optional `DownloadPriority? Priority`
- **FunkArr.Persistence**: New `DownloadMovedWithPriority` event or extend existing
- **FunkArr.Download**: `DownloadManager` handles combined move+priority in one persist cycle; new `Apply` method
- **FunkArr.Api**: Move endpoint accepts optional `priority` in body; `MoveRequest` model extended
- **FunkArr.UI**: Major rebuild of Activity view, new components, new composables, new dependency
- **package.json**: Add `vue-draggable-plus`
