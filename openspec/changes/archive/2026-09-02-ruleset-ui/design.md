## Context

FunkArr has two external API surfaces (Newznab, SABnzbd) but no internal API for the UI. The FunkArr.Api project exists but is empty. The Vue frontend is scaffolded (Vue 3 + Vite + Tailwind v4 + vue-router) but only renders a placeholder. RuleSet data lives across three actors (RuleSetManager, RuleSetResolver, MatchMagicManager) and the MatchHistoryWorker, none of which expose query interfaces for listing or detail views.

## Goals / Non-Goals

**Goals:**
- Expose ruleset data through a JSON REST API at `/api/rulesets/*`
- Build UI pages for browsing rulesets, viewing detail (identity + source + merged config), and drilling into scoring history
- Serve the Vue frontend as static files from the .NET host with SPA fallback
- Keep FunkArr.Api as a thin adapter (same philosophy as ArrApi)

**Non-Goals:**
- Editing rulesets through the UI (read-only for now)
- Download queue/history UI
- Search UI (covered by Sonarr/Radarr through ArrApi)
- Authentication on the internal API (future concern)
- WebSocket/SignalR push updates

## Decisions

### Query messages live in FunkArr.Messages, not in domain projects

The API project references Core which references Messages. New query/response records go in `FunkArr.Messages/RuleSet/` alongside the existing messages. This follows the established pattern — all inter-actor communication is defined in Messages.

Alternative: Define queries inside the actor classes as nested records (like `RuleSetManager.ScanRuleSets`). Rejected because the API project needs to reference these types, and it must not reference domain projects directly.

### RuleSetManager handles detail queries by re-reading files

When the API asks for a specific ruleset's detail, the RuleSetManager re-reads the community/local JSON files and runs `RuleSetMerger.Build()` + `RuleSetMerger.ExtractIdentity()` to produce the response. This is slightly redundant (the worker already did this at load time) but avoids adding state storage to the Manager for data it doesn't need during normal operation.

Alternative: Store merged configs in RuleSetManager state. Rejected because the Manager's job is file watching and orchestration — the merged config is a derivative that already lives in MatchMagicManager. Re-reading is cheap (local JSON files, <1KB each).

### RuleSetResolver handles list queries from its existing state

The Resolver already holds the full registration index (ruleSetId → topic, aliases, media IDs). A new `QueryRegisteredRuleSets` message returns all entries. No new state needed.

### API endpoints use IActorRegistry to resolve actors, Ask pattern for queries

Same pattern as ArrApi: endpoint handlers resolve actors from `IActorRegistry` and use `Ask<T>` with a timeout. No intermediate service layer.

### Static files served from embedded Vite dist/

The Vite build output (`FunkArr.UI/dist/`) is served via `UseStaticFiles` + SPA fallback. In development, Vite dev server runs separately and the .NET host proxies or the frontend hits the API directly via CORS/proxy config.

The `FunkArr.csproj` includes `dist/` as static web assets. The `ApplicationSetupContainer` adds `UseStaticFiles()` before endpoint mapping and a fallback route for SPA routing.

### Vue UI uses fetch for API calls, no state management library

Simple `fetch()` calls to `/api/*` endpoints. Component-local state with Vue 3 Composition API. No Pinia/Vuex — the data is read-only and page-scoped, no shared state needed.

## Risks / Trade-offs

- **[Re-reading files on detail query]** → Files are small JSON (<1KB), re-reading is fast. If a file is deleted between the Manager's known state and the query, the response returns a "not found" status. Acceptable for a diagnostic UI.
- **[No auth on /api]** → The internal API is unauthenticated. FunkArr runs in a trusted home network (Docker). If exposed publicly, a reverse proxy should handle auth. Future change can add API key auth matching the ArrApi pattern.
- **[Ask timeout on queries]** → If an actor is unavailable (restarting, passivated), the API returns 504. The UI should handle this gracefully with retry/error display.

## Open Questions

None — scope is clear from the exploration.
