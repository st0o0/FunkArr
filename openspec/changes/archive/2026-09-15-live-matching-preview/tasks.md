## 1. Client-Side Matching Composable

- [x] 1.1 Create `src/FunkArr.UI/src/composables/useRulesetMatcher.ts` — accepts reactive `candidates` and `rules` refs, returns reactive `{ matchedCount, total, results }` with 500ms debounce
- [x] 1.2 Implement regex matching logic per strategy: `seasonAndEpisodeNumber` (run seasonRegex/episodeRegex), `byAbsoluteEpisodeNumber` (run episodeRegex), `itemTitleExact`/`itemTitleIncludes` (match title rules), `itemTitleEqualsAirdate` (run airdate pattern)
- [x] 1.3 Extract capture groups from regex matches and return as `extractions` (season, episode, airdate, title)

## 2. Auto-Fetch on Topic Change

- [x] 2.1 Create `src/FunkArr.UI/src/composables/useMediathekAutoFetch.ts` — watches a Topic ref, debounces 800ms, calls `GET /api/mediathek/search`, caches results in a reactive ref, exposes loading/error state and a manual refresh function
- [x] 2.2 Wire auto-fetch into `RuleSetBuilder.vue` — connect the Topic field to the auto-fetch composable, pass cached candidates to the matcher composable

## 3. Live Preview Panel

- [x] 3.1 Create `src/FunkArr.UI/src/components/LiveMatchPreview.vue` — replaces the dual-tab DebuggerPanel with a single live preview panel showing candidate list with match indicators
- [x] 3.2 Implement candidate card with match state — green left border + rule ID + extractions for matched, gray left border + "No Match" for unmatched, sorted matched-first
- [x] 3.3 Add panel header with match summary ("8 / 30 matched"), fetch status, and Refresh button
- [x] 3.4 Add empty states — "Enter a Topic to fetch candidates" when no topic, "Add rules to see matches" when no rules, "Fetching candidates..." during load

## 4. Full Test Integration

- [x] 4.1 Add "Full Test" button to LiveMatchPreview — sends cached candidates + builder state to `POST /api/rulesets/test`, replaces live preview with server-side results
- [x] 4.2 Show mode indicator — "Live Preview" with disclaimer vs "Full Test Results"
- [x] 4.3 Reuse existing trace visualization (filter/identification trace expansion) for full test results
- [x] 4.4 Return to live preview when user edits a rule after viewing full test results

## 5. Builder Wiring

- [x] 5.1 Replace DebuggerPanel import in `RuleSetBuilder.vue` with LiveMatchPreview, connect all reactive refs (topic, rules, candidates, match results)
- [x] 5.2 Verify edit mode works — when editing an existing ruleset, auto-fetch with the loaded Topic and show matches against existing rules
