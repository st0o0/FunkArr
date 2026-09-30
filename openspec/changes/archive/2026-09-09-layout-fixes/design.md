## Context

Vue Router supports `meta` fields on routes. A global `afterEach` hook can read the meta and set `document.title`.

## Goals / Non-Goals

**Goals:**
- Each page shows a distinct browser tab title

**Non-Goals:**
- Dynamic titles for detail pages (e.g., "Tatort - FunkArr") - just the page type is enough

## Decisions

### Decision: Route meta + afterEach hook

Add `meta: { title: 'Downloads' }` to each route. In a global `router.afterEach`, set `document.title` to `${to.meta.title} - FunkArr` or just `FunkArr` for the root.

## Risks / Trade-offs

None - minimal change.
