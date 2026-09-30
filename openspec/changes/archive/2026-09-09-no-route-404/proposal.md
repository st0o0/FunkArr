## Why

Navigating to a non-existent route (e.g., `/downloads`, `/settings`, or any typo) shows the sidebar with a completely blank content area. There's no 404 page, no redirect, and no indication that anything went wrong. Users see what looks like a broken app.

## What Changes

- Add a catch-all route to the Vue router that redirects unknown paths to the Dashboard (`/`)
- This follows the pattern used by Sonarr/Radarr which redirect unknown routes to their home page

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `collapsible-sidebar`: Not a spec change - this is a router configuration fix in `main.ts`

## Impact

- **FunkArr.UI**: `main.ts` - add a catch-all route `{ path: '/:pathMatch(.*)*', redirect: '/' }` at the end of the routes array
- No backend changes
