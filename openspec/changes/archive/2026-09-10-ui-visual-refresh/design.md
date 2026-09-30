## Context

FunkArr's Vue 3 + Tailwind v4 UI has 9 views, 12 components, and 6 nav items built incrementally. The visual language is consistent (same tokens everywhere) but has no hierarchy - one button style, micro-text uppercase labels, and a Dashboard that grew into 8 stat cards nobody reads after initial setup. The standalone Search view has no actions. Downloads and History show the same pipeline at different stages.

The backend API surface is stable and complete - this change is UI-only. All existing API endpoints remain unchanged.

## Goals / Non-Goals

**Goals:**
- Restructure IA from 6 nav items to 4 (Overview, Activity, RuleSets, Setup)
- Merge Downloads + History into a single Activity view with tabs
- Move Mediathek search into the RuleSetBuilder as an integrated search + test workflow
- Simplify Dashboard from stat wall to minimal status + recent activity feed
- Establish a visual design system (color tokens, type scale, button tiers, spacing)
- Make the app feel like the best-looking thing in someone's arr stack

**Non-Goals:**
- Light mode / theme switching
- Responsive / mobile layout (desktop-only tool)
- Backend API changes
- New data endpoints
- Internationalization

## Decisions

### 1. Activity view: tabs with shared state vs. route-based tabs

**Decision:** Client-side tabs with shared search/filter state. No sub-routes.

The Activity view has three tabs: Active, Queued, History. Active and Queued share a WebSocket stream (useQueueStream). History uses REST pagination. Tabs switch which data source renders, but the outer shell (header, search) stays mounted.

Route structure: `/activity` only. Tab state is a reactive ref, not a route param. Reason: tab switches should be instant (no re-mount), and Active/Queued share the same SSE connection which shouldn't reconnect on tab change.

History's category filter and search carry over from the current History view. Search input is shared across tabs but only filters History (Active/Queued have no server-side search).

Alternative considered: `/activity/active`, `/activity/queued`, `/activity/history` sub-routes. Rejected because the SSE connection lifecycle would need careful management across route transitions, and deep-linking to a specific tab isn't a real use case.

### 2. Overview layout: what to keep from Dashboard

**Decision:** Keep health status, storage, and active downloads. Drop history stats and cache stats. Add recent completions/failures feed.

Rationale for cuts:
- History stats (completed: 247, success rate: 94%, avg time, total size) - cumulative numbers with no time context. You can't tell if 94% is from last week or last year. Not actionable.
- Cache stats (TVDB: 12, TMDB: 8, oldest entry) - system internals. Useful for debugging, not monitoring. Move to Setup page's health section if wanted later.

What stays:
- Health status line (system healthy / warnings / failures) - directly actionable
- Storage bar (used/total) - directly actionable when disk fills up
- Active downloads with progress - "is anything happening now?"

What's new:
- Recent activity feed - last ~10 completed/failed downloads with timestamp. This is what the user actually wants: "what happened since I last looked?"

The recent feed reuses the existing `getHistory(0, 10)` endpoint. No new API needed.

### 3. Search integration into RuleSetBuilder

**Decision:** Replace the DebuggerPanel's right pane with a two-mode panel: Search tab and Test tab.

Current state: The DebuggerPanel already calls `/api/rulesets/test` with manually constructed test candidates. The standalone Search calls `/api/mediathek/search`. The RuleSetBuilder's `rulesets.ts` already exports a `searchMediathek` function for the debugger's candidate loading.

New flow:
1. Right pane has two tabs: "Search" and "Test Results"
2. Search tab: Mediathek search input + channel/topic filters + results list
3. Each search result has a "Use as test candidate" toggle or select-all
4. "Test Rules" button runs `/api/rulesets/test` with selected candidates against current form state
5. Test Results tab shows per-candidate match/fail with rule traces (current DebuggerPanel output)

The existing `searchMediathek` and `testRuleSet` API functions in `rulesets.ts` already handle both calls. The standalone `mediathek.ts` API module becomes unused and can be deleted.

Alternative considered: Inline search results with immediate test feedback (no separate Test tab). Rejected because the test results are dense (per-rule traces) and mixing them into the search list would be overwhelming.

### 4. Design system: token approach

**Decision:** CSS custom properties in `@theme` block (Tailwind v4 native), no component library, no utility class extraction.

The current `style.css` already uses `@theme` for tokens. We extend this pattern:

```
Surfaces (adjusted):
  --color-surface-base: #0f0f11
  --color-surface-raised: #18181b
  --color-surface-elevated: #27272a
  --color-surface-overlay: #3f3f46

Brand (simplified from 7 to 2):
  --color-accent: #f59e0b
  --color-accent-dim: #b45309

Text (adjusted for contrast):
  --color-text-primary: #fafafa
  --color-text-body: #a1a1aa
  --color-text-secondary: #71717a
  --color-text-muted: #52525b
```

Typography via Google Fonts Inter, loaded in index.html. Fallback to system sans-serif.

Button tiers are Tailwind utility compositions in templates, not extracted CSS classes. Reason: only ~15 buttons in the entire UI, abstraction isn't worth it.

### 5. Sidebar: expanded default, Setup relocation

**Decision:** Default `collapsed = false`. Keep collapse toggle. Setup gets a gear icon separated by a border-t at sidebar bottom.

The localStorage key stays `funkarr-sidebar` for continuity - existing users who set a preference keep it. Only the default changes from collapsed to expanded.

Setup moves from the `systemNav` array to a dedicated slot below the nav, above the collapse toggle. It renders as a gear icon + "Setup" label (or just icon when collapsed).

### 6. Route migration: redirects for old paths

**Decision:** Add route redirects for removed paths.

```
/queue    → /activity (with tab=active)
/history  → /activity (with tab=history)  
/search   → /rulesets
```

These are SPA routes only (no SEO concern), but browser history and bookmarks should not break.

## Risks / Trade-offs

**[Activity tab state complexity]** Active/Queued share an SSE connection, History uses REST. Switching tabs must not disconnect the stream.
- Mitigation: SSE composable stays mounted at Activity level, tabs only control which template renders.

**[RuleSetBuilder pane gets heavier]** Adding search to the right pane increases its cognitive load.
- Mitigation: Two distinct tabs (Search / Test Results) keep concerns separated. Search is input-focused, Test Results is output-focused.

**[Overview "recent activity" may show stale data]** The feed uses a one-shot REST call, not streaming.
- Mitigation: Acceptable - Overview is a check-in page, not a live dashboard. User can navigate to Activity for real-time. Refresh on page focus via `visibilitychange` event.

**[History stats deletion]** Users who valued the cumulative stats lose them.
- Mitigation: These numbers are still available via the API. They can return later in a more contextualized form (e.g., "47 completed this week, 3 failed" with time windows).
