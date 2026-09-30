## Context

FunkArr's Vue 3 frontend has a tab-based navigation (Queue, History, Rulesets, Matches, Settings) with system health checks buried in the Settings view. The match intelligence pipeline captures rich trace data (MatchedTrace, FilteredTrace, UnmatchedTrace with per-rule RuleFailure details), but the UI surfaces this as raw field names and terse codes. Users cannot quickly understand why something matched or didn't, and have no at-a-glance system health indicator.

The backend data model requires minimal changes — the trace data already contains everything needed. The work is primarily frontend presentation plus one small backend filter addition.

## Goals / Non-Goals

**Goals:**
- At-a-glance system health status in the header bar
- Human-readable match evaluation stories with full trace visualization
- Per-rule effectiveness stats on ruleset detail views
- Topic-scoped match history for debugging specific shows

**Non-Goals:**
- Real-time WebSocket push for match events (polling is sufficient)
- Match record editing or manual override from the UI
- Historical trend charts for match quality over time
- Toast/snackbar notification system

## Decisions

### D1: Header layout — gear icon replaces Settings tab

Remove "Settings" from the `tabs` array in `App.vue`. Add a gear icon with a status dot to the right side of the header. The icon is a `<router-link to="/settings">` with an adjacent dot element.

**Status dot logic:** Poll `/api/v1/setup/status` every 30 seconds via `usePolling`. Map the response to three states:
- **Green**: `configured && ffmpeg.found && paths.downloadOk && paths.tempOk && mediathek.reachable`
- **Red**: `!configured || !ffmpeg.found` (critical — service won't function)
- **Amber**: everything else (degraded but operational)

**Why not a dropdown/popover?** Keeps it simple — clicking navigates to the full Settings view where details are already shown. No new component needed.

### D2: Match detail as a routed view, not modal

Add route `/matches/:id` → `MatchDetailView.vue`. The Recent list in MatchesView links each record to this route.

**Why route over modal?** Linkable URLs, browser back-button works, can be opened in new tab. The detail view has enough content (3 sections × multiple items) that a modal would feel cramped.

**Data source:** The detail view receives the `id` param, fetches all recent records from `/api/v1/matches/recent`, and finds the matching record client-side. The 100-record cap makes this practical. No new backend endpoint needed for individual record lookup.

**Alternative considered:** `GET /api/v1/matches/:id` endpoint. Rejected because it would require indexing by ID in RecentMatchActor state, adding complexity for a lookup that works fine client-side given the small dataset.

### D3: Match detail layout — storytelling structure

The view tells the story of one search evaluation in three layers:

1. **Header**: topic, season/episode (if TV), timestamp, source badge, total results count
2. **Summary bar**: three stat boxes (matched count green, filtered count amber, unmatched count red)
3. **Trace sections** (collapsible, expanded by default):
   - **Matched**: each item shows title, rule index → strategy name → confidence badge → resolved episode. Confidence colored: ≥0.8 green, ≥0.5 amber, <0.5 red.
   - **Filtered**: each item shows title, reason as human-readable label (e.g. "accessibility-skip" → "Accessibility variant skipped"), filter field/op/value vs. actual value.
   - **Unmatched** (collapsed by default if >5 items): each item shows title, then a pipeline visualization of rule failures in order.
4. **Footer**: link to ruleset detail view

### D4: Unmatched pipeline visualization

For each unmatched item, show the rule evaluation pipeline as a vertical step list:

```
Rule #0 ─ ✕ filter-failed: duration > 15 min (actual: 3 min)
Rule #1 ─ ✕ strategy-no-match: SeasonAndEpisodeNumber
```

Each step shows rule index, failure icon, failure reason, and the detail string. This makes it immediately obvious where in the pipeline the item fell out.

### D5: Per-rule hit stats on RulesetDetail

Fetch `TopicStats` via `GET /api/v1/matches/topics/{tvdbId}` and extract `perRuleHitCounts`. Display on each `RuleCard` as a mini bar with count and percentage. The `perRuleHitCounts` dictionary key is stringified rule index (e.g. `"0"`, `"1"`), matching the `RuleIndex` on MatchedTrace.

**Fallback:** If no stats exist yet (new ruleset, no searches), show "No match data yet" in muted text.

### D6: Topic filter on recent matches endpoint

Add optional `topic` query parameter to `GET /api/v1/matches/recent`. Filter in `RecentMatchActor.GetRecent` by comparing `MatchRecord.SearchTopic` case-insensitively. This is a LINQ `.Where()` on the in-memory list — no persistence changes.

### D7: Reuse existing composables and patterns

- `usePolling` for the header status dot (30s interval)
- Same Tailwind utility patterns as existing views (neutral backgrounds, colored text/badges, rounded cards)
- Same `api()` client wrapper for all fetch calls

## Risks / Trade-offs

**[Client-side match lookup]** Finding a MatchRecord by ID from the full `/recent` response means fetching up to 100 records to display one. → Acceptable given the small payload size. If the record is no longer in the 100-record window, show a "Record no longer available" message.

**[Polling frequency for status dot]** 30s is a balance between freshness and request volume. → If the status endpoint is slow (e.g. mediathek reachability check), the 30s poll won't block the UI since `usePolling` is async.

**[perRuleHitCounts key format]** Assumed to be stringified rule index. → Verify in implementation. If it's a different key format, the mapping to RuleCard needs adjustment.
