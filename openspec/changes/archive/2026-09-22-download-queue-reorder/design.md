## Context

The download queue in `DownloadManager` is a flat `IReadOnlyList<Guid>` with strict FIFO ordering. There is no priority concept — new downloads append to the end, `DispatchNext()` takes the first available item. The only queue-jumping mechanism is `ForceStartDownload`, which bypasses the concurrency limit entirely.

The SABnzbd-compatible API accepts a `priority` query parameter on `addfile` but ignores it. Queue slots report hardcoded `"Normal"` priority. The `mode=queue` handler has no `name=switch` or `name=priority` support.

Sonarr and Radarr map their priority profiles to SABnzbd's integer scale (-1/0/1/2) when submitting downloads. Without support, all downloads are treated identically regardless of what the *arr apps request.

## Goals / Non-Goals

**Goals:**

- Priority-ordered dispatch: High-priority downloads dispatch before Normal, Normal before Low
- Manual queue reordering via move-to-position (supports drag-and-drop UI)
- Swap operation for exchanging two items within the same priority bucket
- SABnzbd API compatibility for priority setting, queue switching, and priority-aware addfile
- Priority preserved through dispatch and recovery cycles

**Non-Goals:**

- UI implementation for drag-and-drop or priority controls (separate change)
- Priority-based bandwidth allocation or speed throttling
- Per-category default priorities
- Automatic priority escalation based on age or wait time
- Migration of existing persistence journals (0.2.0 breaking change)

## Decisions

### 1. Single sorted list with QueueEntry (not separate bucket lists)

Queue state uses one `IReadOnlyList<QueueEntry>` where `QueueEntry(Guid Id, DownloadPriority Priority)` is a readonly record struct. The list is kept sorted by priority (High first, then Normal, then Low). Within each priority bucket, order is determined by insertion/move position.

**Why not separate bucket lists?** A single list means `DispatchNext()` stays unchanged — it iterates from the front and takes the first available item. Move-to-position works on a single index space. No bucket-routing logic needed. The sort invariant is maintained by the Apply methods, not by a separate sort step.

**Why readonly record struct?** Value type semantics, no heap allocations for queue entries, and clean pattern matching.

### 2. Dispatched as Dictionary preserving priority

`Dispatched` changes from `IReadOnlySet<Guid>` to `IReadOnlyDictionary<Guid, DownloadPriority>`. This preserves priority through the dispatch cycle so that `ResetDispatched()` (called on recovery) can reconstruct `QueueEntry` values with the correct priority instead of defaulting to Normal.

The priority is looked up from the `Queued` list during `Apply(DownloadDispatched)` — it is not duplicated in the event. This keeps events DRY while the state carries what recovery needs.

### 3. Global position index with bucket clamping

`MoveDownload(id, position)` uses a global queue index (0-based). The Apply method clamps the target position to the item's priority bucket boundaries. A Normal-priority item moved to position 0 lands at the start of the Normal bucket, not in the High zone.

**Why global, not bucket-relative?** The UI sees the full queue as one list. Drag-and-drop produces a global target index naturally. SABnzbd's `move` also uses a global slot index. The clamp is transparent — the caller doesn't need to know bucket boundaries.

### 4. Swap restricted to same priority bucket

`SwapDownloads(id1, id2)` exchanges positions of two items. Both must have the same priority — cross-bucket swap returns a failure response. Swapping items between priority levels would create position/priority contradictions (an item's visual position wouldn't match its dispatch order).

If cross-bucket repositioning is needed, the caller should use `SetDownloadPriority` to change the priority first, which re-inserts the item at the end of the target bucket.

### 5. Priority change inserts at end of target bucket

`SetDownloadPriority(id, newPriority)` removes the item from its current position and inserts it at the end of the target priority bucket. This is consistent with SABnzbd's behavior — changing priority to High makes the item important, but it doesn't jump ahead of other High items that were already waiting.

### 6. Force stays a separate mechanism

SABnzbd's Force priority (integer value 2) maps to the existing `ForceStartDownload` command rather than becoming a `DownloadPriority.Force` enum value. Force means "start immediately, bypass concurrency limit" — it's an action, not a queue position. Force-started items are in `Dispatched`, not `Queued`, and don't participate in priority ordering.

The SABnzbd priority handler maps value 2 to `ForceStartDownload` and values -1/0/1 to `SetDownloadPriority`.

### 7. DownloadEnqueued carries priority (breaking, 0.2.0)

The `DownloadEnqueued` event gains a `DownloadPriority Priority` field. Since this is a 0.2.0 breaking change, no default value or migration is needed — old journals are not replayed.

### 8. DispatchNext unchanged

Because the queue list is pre-sorted by priority, `DispatchNext()` continues to iterate from the front and dispatch the first non-dispatched items up to the concurrency limit. No priority-aware logic needed in the dispatch path — the sort invariant guarantees correct order.

## Risks / Trade-offs

**[List operations are O(n)]** The queue is an immutable list. Move, swap, and priority change all create new lists via LINQ. For typical queue sizes (tens to low hundreds of items) this is negligible. → No mitigation needed at current scale.

**[Bucket boundaries computed on every Apply]** Finding the start/end of a priority bucket requires scanning the list. → Acceptable for the same size argument. Could be cached if queues grow to thousands.

**[SABnzbd switch may be called cross-bucket by external tools]** If Sonarr/Radarr ever issue a switch between items of different priorities, the operation fails. → Return SABnzbd-compatible error response. This is unlikely — the *arr apps rarely use switch directly.

**[No persistence migration]** Old journals are incompatible with the new event shape. → This is a 0.2.0 breaking release. Users must clear persistence state when upgrading. Document in release notes.
