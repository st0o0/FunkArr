## Why

The Queue page (/queue) has three bugs that degrade the primary download monitoring experience:

1. **SSE stream not reconnecting after SPA navigation**: Navigating from Dashboard to Queue via Vue Router sometimes shows an empty page. The `useQueueStream` singleton uses a `refCount` pattern, but the `connect()` guard `if (eventSource) return` can silently skip reconnection if the EventSource is in a closed/error state. A hard page refresh fixes it.

2. **Every download renders as its own 1-item group**: The grouping logic groups by parsed series name, but since each download is a unique episode, every group has exactly 1 item - creating redundant group headers with collapse arrows that add visual noise without value.

3. **Group collapse arrow doesn't respond**: While the click handler is wired correctly in code (`@click="expanded = !expanded"`), the reactive `expanded` ref initializes from `props.defaultExpanded` which only reads once. The `Expand All` toggle on the parent changes `allExpanded` but the child's `expanded` ref is already initialized and doesn't track prop changes.

## What Changes

- Fix SSE reconnection: check EventSource `readyState` instead of just null-checking the variable. If closed/error, close and reconnect.
- Remove group wrapper for single-item groups: render `QueueCard` directly when a group has exactly 1 item, keep grouping for multi-item groups only.
- Fix Expand All: use a `watchEffect` or computed to sync `defaultExpanded` prop changes into the local `expanded` ref.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `download-queue-ui`: Fix SSE reconnection, flatten single-item groups, sync expand/collapse state

## Impact

- **FunkArr.UI**: `useQueueStream.ts` - fix reconnection logic
- **FunkArr.UI**: `Queue.vue` - conditional rendering for single vs multi-item groups
- **FunkArr.UI**: `QueueGroupCard.vue` - watch `defaultExpanded` prop changes
- No backend changes
