## Context

The RuleSet Builder has a split-pane layout: form on the left, debugger panel on the right. The debugger panel currently has two tabs — Search (Mediathek search with candidate selection) and Test Results (server-side scoring trace). Users must manually search, select candidates, and click "Test Rules" to see results.

The existing APIs (`GET /api/mediathek/search` and `POST /api/rulesets/test`) are sufficient. No backend changes needed.

## Goals / Non-Goals

**Goals:**
- Instant feedback when editing regex patterns, filters, or strategies
- Zero-click matching: auto-fetch candidates when Topic is set, auto-match when rules change
- Keep the full server-side test pipeline available for accurate scoring validation
- Smooth UX: no flashing, no stale results, clear loading states

**Non-Goals:**
- Client-side filter evaluation (too complex to replicate — leave for server-side full test)
- Client-side scoring/confidence calculation
- Replacing the server-side test entirely (it's the source of truth)
- Regex syntax differences between JS and .NET (simple patterns used in rulesets are compatible)

## Decisions

### D1: Panel redesign → Single-panel with live results + expandable full test

**Choice**: Replace the two-tab layout (Search / Test Results) with a single continuous panel:
1. Top: Topic-driven auto-fetch status (showing cached candidate count)
2. Middle: Live match results list — each candidate shows match/no-match with extracted values
3. Bottom: "Full Test" button for server-side pipeline validation
4. Expandable: Each candidate card expands to show the full rule pipeline trace (from server-side test)

**Why**: Two tabs create friction. The user shouldn't have to switch between searching and seeing results. A single panel with live results is more direct.

### D2: Data flow → Topic triggers fetch, rules trigger client-side match

```
Topic changes (debounced 800ms)
  → GET /api/mediathek/search?q={topic}&limit=30
  → Cache candidates in reactive ref

Rules change (debounced 500ms)
  → For each cached candidate:
    → For each rule with regex patterns:
      → Run RegExp.exec() against candidate title/topic
      → Extract capture groups
      → Determine match/no-match
  → Update live results reactively
```

**Why**: Separating fetch (expensive, external API) from match (cheap, local) gives instant regex feedback without hitting the network.

### D3: Client-side matching → Composable `useRulesetMatcher`

**Choice**: Extract matching logic into a Vue composable `useRulesetMatcher(candidates, rules)` that returns reactive match results.

The composable handles:
- Watching `candidates` and `rules` refs
- Debouncing rule changes (500ms)
- Running regex matching per candidate per rule
- Returning `{ matchedCount, results: [{ candidate, matchedRule, extractions }] }`

**Matching logic** (simplified, per candidate per rule):
1. Check strategy type
2. For `itemTitleExact` / `itemTitleIncludes`: run title rule patterns against candidate fields
3. For `seasonAndEpisodeNumber` / `byAbsoluteEpisodeNumber`: run `seasonRegex` / `episodeRegex` against title
4. For `itemTitleEqualsAirdate`: run airdate regex patterns against title
5. Report: which rule matched, what was extracted

**Not replicated client-side**: Filter evaluation (field comparisons like duration > 60) — these show as "Filters: server-side only" in the live preview. The full test evaluates them.

### D4: Full test integration → On-demand button, results merge into live view

**Choice**: The "Full Test" button runs `POST /api/rulesets/test` with all cached candidates. Results replace the client-side preview with full pipeline results (including filter evaluation, scoring, and trace). The expanded trace view (already implemented) shows the detailed pipeline.

**Why**: The client-side preview shows "does my regex match?" quickly. The full test answers "does the complete rule (filters + strategy + scoring) work correctly?" — different questions, both valuable.

### D5: Auto-fetch behavior → Only on Topic change, with manual refresh

**Choice**: Auto-fetch when Topic field changes (debounced 800ms). Show a small "Refresh" button to manually re-fetch. Cache results until Topic changes or user clicks Refresh.

**Why**: The Mediathek API is external and relatively slow (~500ms). Auto-fetching on every field change would be wasteful. Topic is the natural trigger because it determines which shows appear in search results.

## Risks / Trade-offs

- **[JS/NET regex differences]** → Mitigation: The patterns in rulesets are simple (character classes, groups, alternation). Named groups and lookbehind differ, but aren't used in practice. Show a disclaimer "Preview — run Full Test for accurate results".
- **[Stale candidates]** → Mitigation: Show fetch timestamp, provide manual Refresh button.
- **[Large result sets]** → Mitigation: Limit auto-fetch to 30 candidates. Client-side matching of 30 items is instant.
