## Context

The Queue page uses a shared SSE composable (`useQueueStream`) with ref-counted connect/disconnect. The `ActiveDownloads` widget on the Dashboard also uses it. When navigating between pages, the old component unmounts (release) and new component mounts (acquire) - the EventSource should transfer seamlessly via the refCount.

## Goals / Non-Goals

**Goals:**
- SSE stream reliably provides data after any SPA navigation path
- Single-item groups render as flat cards without group wrapper
- Expand All/Collapse All works correctly

**Non-Goals:**
- Changing the SSE endpoint or protocol
- Adding WebSocket support
- Changing the grouping key (stays as parsed series name)

## Decisions

### Decision: Check readyState instead of null-guard in connect()

Current: `if (eventSource) return` - skips if variable exists even if closed.
Fix: Check `eventSource?.readyState !== EventSource.OPEN`, and if not open, close the stale instance and create a new one. This handles all edge cases: null, CONNECTING (let it finish), OPEN (skip), CLOSED (reconnect).

### Decision: Conditional rendering in Queue.vue for single-item groups

When `group.items.length === 1`, render `QueueCard` directly wrapped in a simple container (same border/rounded styling). When `group.items.length > 1`, render `QueueGroupCard` with the full collapse header. This removes the redundant "1 downloading" headers.

### Decision: Watch defaultExpanded prop in QueueGroupCard

Replace the one-shot `ref(props.defaultExpanded ?? ...)` with a `watch` on `() => props.defaultExpanded` that syncs the local `expanded` ref when the parent toggles Expand All / Collapse All. The watch should only fire on defined values (not undefined, which means "no override").

## Risks / Trade-offs

**[Risk] Series with exactly 1 item in queue** -> Renders as flat card, loses the series name header. Acceptable because the card already shows the full title with series name parsed.
