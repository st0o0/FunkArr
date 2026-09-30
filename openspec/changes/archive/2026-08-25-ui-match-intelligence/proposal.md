## Why

The match intelligence data (why something matched, which rule fired, why items were filtered or unmatched) is fully captured in the backend but poorly surfaced in the UI. The "Recent" tab shows raw field names (`ruleIndex: 0`, `strategy: "seasonAndEpisodeNumber"`) that mean nothing without cross-referencing the ruleset. The "Unmatched" tab lists failure reasons as terse strings without showing the evaluation pipeline. Additionally, system health status is buried in the Settings view — users have no at-a-glance indicator that something is misconfigured.

## What Changes

- **Header redesign**: Replace the "Settings" tab with a gear icon + status dot (green/amber/red) on the right side of the header bar. The dot polls `/api/v1/setup/status` and reflects system health. Clicking the gear navigates to `/settings`.
- **Match detail view**: Add a dedicated route (`/matches/:id`) that tells the story of a single search evaluation — summary stats, then sections for matched/filtered/unmatched items with human-readable explanations, confidence context, and a link back to the ruleset.
- **Per-rule hit stats on RulesetDetail**: Show `perRuleHitCounts` from `TopicStats` on each rule card as a mini progress bar with match count and percentage.
- **Topic-filtered match history**: Add `topic` query parameter to `GET /api/v1/matches/recent` so the frontend can fetch match history for a specific show/topic.

## Capabilities

### New Capabilities

- `header-status-indicator`: Gear icon with health-status dot in the header bar, replacing the Settings tab entry.
- `match-detail-view`: Dedicated route for viewing a single MatchRecord with full trace visualization.

### Modified Capabilities

- `match-views`: Add per-rule hit stats display on RulesetDetail; link from Recent list to match detail route.
- `match-intelligence-api`: Add `topic` query parameter filtering to the recent matches endpoint.
- `web-ui-shell`: Remove Settings from tab list; add gear+status icon to header right side.

## Impact

- **Frontend**: `App.vue` (header/nav restructure), `MatchesView.vue` (link to detail), new `MatchDetailView.vue`, `RulesetDetail.vue` (per-rule stats), `router.ts` (new route), `types.ts` (if any new response shapes).
- **Backend**: `RecentMatchActor` (topic filter on GetRecent), `MatchIntelligenceController` (query parameter), `ContractMappingExtensions` (if response shape changes).
- **No persistence changes** — all data already exists in MatchRecord/TopicStats.
- **No breaking API changes** — topic filter is additive (optional query param).
