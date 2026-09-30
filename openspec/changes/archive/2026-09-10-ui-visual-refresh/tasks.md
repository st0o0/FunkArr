## 1. Design System Foundation

- [x] 1.1 Rewrite `style.css`: updated surface tokens (#0f0f11/#18181b/#27272a/#3f3f46), single accent (#f59e0b + #b45309), slate secondary (#64748b), adjusted text colors (#fafafa/#a1a1aa/#71717a/#52525b), borders at 0.08 opacity, Inter font via Google Fonts
- [x] 1.2 Add Inter font import to `index.html`, update `--font-sans` to `'Inter', -apple-system, ...`
- [x] 1.3 Audit all views and components: replace `text-[10px]` with `text-[11px]`, replace `text-[11px]` descriptions with `text-xs` (12px), remove all `uppercase tracking-widest` section labels, apply tighter heading tracking

## 2. Sidebar & Navigation Restructure

- [x] 2.1 Rewrite AppLayout.vue nav: flat list with Overview, Activity, RuleSets (3 items), remove "Media"/"System" section headers
- [x] 2.2 Move Setup to dedicated bottom slot with gear icon, separated by border-t, above collapse toggle
- [x] 2.3 Change sidebar default from `collapsed = true` to `collapsed = false` (keep localStorage override)
- [x] 2.4 Update `main.ts` routes: rename `/` to Overview, add `/activity` route, add redirects for `/queue` -> `/activity`, `/history` -> `/activity`, `/search` -> `/rulesets`

## 3. Activity View (Merge Downloads + History)

- [x] 3.1 Create `Activity.vue` with three client-side tabs (Active, Queued, History), mount `useQueueStream` at view level so SSE survives tab switches
- [x] 3.2 Implement Active tab: reuse QueueGroupCard/QueueCard for grouped download display with progress, cancel, overall progress bar
- [x] 3.3 Implement Queued tab: list waiting items with title, channel, category, size, cancel button (no progress bars)
- [x] 3.4 Implement History tab: migrate table from History.vue with pagination, category filter (via `/api/downloads/history/categories`), retry, delete
- [x] 3.5 Add shared search input above tabs that filters History results client-side, add badge counts to Active/Queued tabs from SSE data
- [x] 3.6 Delete `Queue.vue` and `History.vue`

## 4. Overview (Simplify Dashboard)

- [x] 4.1 Rewrite `Home.vue` as Overview: remove 8 stat cards row and cache stats section
- [x] 4.2 Add compact status line: health dot + text ("System healthy" / "1 warning" / "2 failures") linking to Setup, storage bar (used/total with warn color at 90%)
- [x] 4.3 Add queue progress line: "N downloading - N queued - speed" with overall progress bar, or "No active downloads" when idle
- [x] 4.4 Add recent activity feed: fetch last 10 history items, show status icon (green check/red cross) + title + relative timestamp, refetch on visibilitychange
- [x] 4.5 Remove HealthWidget and ActiveDownloads component imports from Overview (functionality replaced by status line and activity feed)

## 5. Builder Search Panel (Integrate Search into RuleSetBuilder)

- [x] 5.1 Rework RuleSetBuilder right pane: replace DebuggerPanel with dual-tab panel (Search tab + Test Results tab)
- [x] 5.2 Implement Search tab: search input + channel/topic filters, debounced Mediathek API call, scrollable results list with candidate cards (title, topic, channel, duration, quality)
- [x] 5.3 Add candidate selection: checkbox per result, "Select All" toggle, selected count display
- [x] 5.4 Add "Test Rules" button: serialize builder form + selected candidates to `POST /api/rulesets/test`, disabled when no candidates or no rules, auto-switch to Test Results on completion
- [x] 5.5 Implement Test Results tab: sorted results (matched first), per-candidate cards with match/fail badges, expandable rule traces with filter/identification details
- [x] 5.6 Delete standalone `Search.vue` and `api/mediathek.ts` (search function already exists in `api/rulesets.ts`)

## 6. Visual Polish Pass

- [x] 6.1 Apply button tiers across all views: Primary (amber bg, black text) for Save/Confirm/Next/Test, Secondary (elevated bg, border) for Cancel/Back, Ghost (text-only) for inline actions
- [x] 6.2 Apply consistent max-widths: `max-w-3xl mx-auto` for Overview/Activity/RuleSetList/RuleSetDetail/ScoringHistory/Setup, `max-w-5xl` for RuleSetBuilder/ScoringDetail
- [x] 6.3 Standardize page padding to `px-6 py-5` in AppLayout main area
- [x] 6.4 Update RuleSetList, RuleSetDetail, ScoringHistory, ScoringDetail, Setup: replace uppercase section headers with normal-case `text-sm font-semibold`, apply new text color tokens
- [x] 6.5 Remove `animate-pulse` from QueueCard download status dot, use static color distinction instead

## 7. Cleanup

- [x] 7.1 Remove unused brand token shades (brand-300/400/700/900 and accent-400/500) from style.css
- [x] 7.2 Verify all routes work including redirects, test tab switching in Activity, test Builder search+test flow
- [x] 7.3 Run `npm run build` to verify no dead imports or missing components
