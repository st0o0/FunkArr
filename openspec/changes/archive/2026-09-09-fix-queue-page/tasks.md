## 1. Fix SSE reconnection

- [x] 1.1 In `useQueueStream.ts`, update `connect()` to check `eventSource.readyState` - if CLOSED, close and recreate; if CONNECTING or OPEN, return early
- [x] 1.2 Clear `items.value` to empty array on disconnect to avoid showing stale data

## 2. Flatten single-item groups

- [x] 2.1 In `Queue.vue`, add conditional rendering: if `group.items.length === 1`, render a standalone `QueueCard` wrapped in a simple rounded border container; otherwise render `QueueGroupCard`
- [x] 2.2 Wire the cancel emit from the standalone card to `handleCancel`

## 3. Fix Expand All propagation

- [x] 3.1 In `QueueGroupCard.vue`, add a `watch` on `() => props.defaultExpanded` that sets `expanded.value` when the prop is explicitly `true` or `false` (not undefined)

## 4. Verify

- [x] 4.1 Build the UI (`npm run build` in FunkArr.UI) to verify no errors
