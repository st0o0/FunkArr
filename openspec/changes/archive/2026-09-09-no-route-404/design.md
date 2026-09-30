## Context

The Vue router in `main.ts` has 10 explicit routes. Any path not matching these renders the `AppLayout` shell with no component in the `<router-view>` slot, producing a blank content area.

## Goals / Non-Goals

**Goals:**
- Unknown routes redirect to the Dashboard instead of showing blank

**Non-Goals:**
- A dedicated 404 page (redirect is simpler and matches *arr conventions)
- Deep-link preservation (unknown routes are genuinely wrong, not stale bookmarks to preserve)

## Decisions

### Decision: Redirect to root instead of 404 page

A redirect to `/` is simpler, matches what Sonarr/Radarr do, and avoids creating a new view component for an edge case. The user lands on the Dashboard which shows system status.

## Risks / Trade-offs

None significant. This is a one-line router change.
