## Context

The download queue backend now supports priority buckets (High/Normal/Low), move-to-position, swap, and set-priority operations. The frontend was built before these features and shows a flat queue list split across separate Active/Queued tabs, with no priority display, no reorder controls, and missing API bindings for pause/resume/force-start.

Current UI stack: Vue 3.5, Vite 8.2, TypeScript 6.0, Tailwind 4 (custom dark theme, no component library). No DnD library installed. SSE stream polls queue every 3 seconds.

## Goals / Non-Goals

**Goals:**

- Single unified queue view with active downloads pinned at top and priority-bucketed waiting queue below
- Drag-and-drop reordering within and across priority buckets using vue-draggable-plus
- Cross-bucket drag atomically changes priority and position in one API call
- Context menu for priority changes, force start, and delete
- Pipeline pause/resume controls with visual state indication
- SSE buffering during drag to prevent layout jumps
- Optimistic UI updates with revert on failure
- Complete API client for all queue operations

**Non-Goals:**

- Mobile/touch drag-and-drop optimization (desktop-first, touch works via SortableJS fallback)
- Keyboard-only reordering (future accessibility improvement)
- Queue item batch selection/operations
- Drag-and-drop between active and queued sections (active items are not movable)

## Decisions

### 1. Merge Active + Queued into one Queue tab

The Activity view changes from 3 tabs (Active/Queued/History) to 2 tabs (Queue/History). The Queue tab shows active downloads in a non-draggable section at the top, followed by priority-bucketed queue sections below. This matches SABnzbd's approach where everything is one list.

**Why?** Separating active and queued downloads forces users to switch tabs to see the full pipeline. The priority ordering only makes sense when you see the whole picture — what's running, what's next.

### 2. vue-draggable-plus for DnD

Vue-draggable-plus is a modern SortableJS wrapper with native Composition API support. It handles grouped lists — each priority bucket is a sortable group that accepts items from other groups. When an item moves between groups, SortableJS fires `onAdd`/`onRemove` events that map directly to our cross-bucket move API.

**Why not native HTML5 DnD?** No group support, poor UX (no animation, no auto-scroll), much more code for the same result. VueDraggable handles the hard parts: ghost elements, placeholder positioning, scroll during drag.

### 3. Atomic move+priority endpoint

Cross-bucket drag needs to change priority AND set position in one request. The existing `POST /api/downloads/queue/{id}/move` is extended to accept an optional `priority` field in the body. The backend performs both operations in one persist cycle — no intermediate state visible to SSE subscribers.

```
POST /api/downloads/queue/{id}/move
Body: { "position": 2 }                    — same-bucket move (existing)
Body: { "position": 0, "priority": "High" } — cross-bucket move (new)
```

This avoids the two-request race condition (set priority → SSE update → move) and keeps the frontend simple.

### 4. SSE buffering during drag

The SSE composable (`useQueueStream`) exposes an `isDragging` ref. While true, incoming SSE events are stored in a buffer instead of updating the reactive queue state. On drop, the buffer is flushed: the latest buffered state replaces the current state, then the optimistic move result is re-applied if the API call hasn't returned yet.

**Why not just ignore SSE during drag?** We need the buffered state for reconciliation. The optimistic UI might be wrong if another user (or Sonarr) added/removed items during the drag. The buffer ensures we don't lose those changes.

### 5. Optimistic UI with revert

Move and priority operations update the local queue state immediately before the API call returns. If the API call fails, the state reverts to the pre-operation snapshot. This makes drag-and-drop feel instant instead of waiting 100-200ms for the round trip.

The composable `useDragQueue` manages this: it takes a snapshot before applying the optimistic change, then either commits (on success) or reverts (on failure).

### 6. Context menu instead of inline buttons

Each queue item has a three-dot (⋮) button that opens a context menu with: priority radio group (High/Normal/Low with current selection marked), Force Start action, and Delete action. Active downloads only show Delete.

**Why context menu?** Inline buttons clutter the card layout and don't scale — showing priority + force + delete on every card wastes space. A context menu keeps the card clean and is consistent with the *arr ecosystem (Sonarr/Radarr use the same pattern).

### 7. Priority section visibility

Priority bucket sections (headers + item lists) are hidden when their bucket is empty. During an active drag, empty buckets appear as drop zones so users can drag items into them. After drop, empty buckets hide again.

### 8. Component architecture

```
Activity.vue
├── QueueTab (inline in Activity.vue)
│   ├── PipelineStatus (pause/resume, state display)
│   ├── ActiveSection
│   │   └── ActiveDownloadCard.vue (per item, progress bar, no drag)
│   ├── PrioritySection.vue × 3 (High/Normal/Low)
│   │   └── QueueCard.vue (per item, drag handle, context menu)
│   └── QueueContextMenu.vue (shared, positioned on trigger)
└── HistoryTab (existing, minimal changes)
```

## Risks / Trade-offs

**[SortableJS bundle size]** vue-draggable-plus adds ~15KB gzipped. → Acceptable for the interaction quality it provides. No lighter alternative offers grouped drag.

**[SSE buffer can grow stale]** If a drag lasts very long (user walks away), buffered state may be significantly outdated. → Accept: drags rarely last more than a few seconds. The post-drop reconciliation handles it.

**[Optimistic revert may flash]** If the API is slow and the next SSE update arrives after revert but before the corrected state, the UI may briefly show a stale order. → The 3-second SSE interval makes this unlikely, and the next tick corrects it.

**[Empty-bucket drop zones during drag]** Showing hidden buckets during drag requires detecting drag start/end globally, not just per-section. → SortableJS `onStart`/`onEnd` events on the parent container handle this.
