## Why

The RuleSet Builder's debugger requires a 4-step manual workflow: search Mediathek, select candidates, click Test, read results. This breaks the creative flow of writing rules — every regex change requires repeating the test cycle. Users can't see the effect of their edits until they explicitly run a test.

## What Changes

- **Live matching preview**: The right panel auto-fetches Mediathek results when the Topic is set, then continuously matches the cached results against the current rules as the user edits. No manual search-select-test cycle needed.
- **Client-side regex preview**: Pattern changes produce instant visual feedback by running `RegExp` matching in the browser against cached candidates. Capture group extractions (season, episode, airdate) are shown inline.
- **Server-side full test on demand**: A "Full Test" button sends the current state to `POST /api/rulesets/test` for the complete pipeline evaluation (filters + strategy + scoring). This validates what the client-side preview can't (filter logic, scoring confidence).
- **Topic-driven auto-fetch**: Changing the Topic field triggers a debounced Mediathek search. Results are cached and reused for all subsequent matching until the Topic changes again.
- **Inline match indicators**: Each cached candidate shows a live match/no-match state with extracted values, updating as the user types.

## Capabilities

### New Capabilities
- `live-matching-preview`: Client-side regex matching of cached Mediathek results with live feedback in the builder panel

### Modified Capabilities
- `builder-search-panel`: Replace the manual search-select-test tabs with a live preview panel that auto-fetches on topic change and shows continuous match results
- `ruleset-debugger-ui`: Adapt trace display to work with both live preview (client-side) and full test (server-side) results

## Impact

- **FunkArr.UI**: `RuleSetBuilder.vue`, `DebuggerPanel.vue`, new composable for client-side matching logic
- **No backend changes**: Existing `GET /api/mediathek/search` and `POST /api/rulesets/test` are sufficient
- **No new dependencies**: `RegExp` is native JavaScript
