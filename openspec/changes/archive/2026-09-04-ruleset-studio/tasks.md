## 1. Messages & Scoring Infrastructure

- [x] 1.1 Add `Test` value to `ScoringOrigin` enum in FunkArr.Messages
- [x] 1.2 Add `TestScoreItems` message record (inline MatchingConfig + ScoreCandidate[]) and `TestScoreCompleted` response (ItemTrace[]) in FunkArr.Messages
- [x] 1.3 Handle `TestScoreItems` in MatchMagicManager — forward to scoring pool as ExecuteScoring with ScoringOrigin.Test
- [x] 1.4 Skip `RecordScoringResult` in MatchMagicActor when Origin is ScoringOrigin.Test
- [x] 1.5 Extract rule transformation logic from RuleSetMerger into a reusable static method `TransformRulesToConfig(string ruleSetId, float defaultConfidence, List<RawRule> rules)` that the test endpoint can call with ad-hoc JSON

## 2. RuleSet Write API

- [x] 2.1 Create `RuleSetWriteApiEndpoints.cs` in FunkArr.Api with `MapRuleSetWriteApi` extension method
- [x] 2.2 Implement `POST /api/rulesets` — validate ruleSetId format + topic, check no local file exists, serialize to JSON, write via IDataFiles.WriteAtomic to DataPaths.LocalRuleSets
- [x] 2.3 Implement `PUT /api/rulesets/{id}` — validate ruleset exists (community or local), write local file via WriteAtomic
- [x] 2.4 Implement `DELETE /api/rulesets/{id}` — check local file exists, remove via IDataFiles.Remove, return 404 if no local file
- [x] 2.5 Add write endpoint registration in ApplicationSetupContainer (call MapRuleSetWriteApi)
- [x] 2.6 Write API tests for create, update, delete endpoints (happy path + error cases)

## 3. Ad-hoc Test & MediathekViewWeb Proxy API

- [x] 3.1 Create `RuleSetTestApiEndpoints.cs` in FunkArr.Api with `POST /api/rulesets/test` — deserialize config + candidates, transform rules via RuleSetMerger, Ask MatchMagicManager with TestScoreItems, return ItemTrace[]
- [x] 3.2 Create `MediathekApiEndpoints.cs` in FunkArr.Api with `MapMediathekApi` extension method and `GET /api/mediathek/search` — accept q + limit params, Ask MediathekViewWebManager, map response to candidate-shaped JSON
- [x] 3.3 Add test and mediathek endpoint registration in ApplicationSetupContainer
- [x] 3.4 Write API tests for test scoring endpoint (match, no-match, filter trace, empty candidates)
- [x] 3.5 Write API tests for mediathek proxy endpoint (results, no results, missing query param)

## 4. Frontend API Client

- [x] 4.1 Add write functions to `api/rulesets.ts`: `createRuleSet`, `updateRuleSet`, `deleteRuleSet`
- [x] 4.2 Add test/debugger functions: `testRuleSet(config, candidates)` calling POST /api/rulesets/test
- [x] 4.3 Add mediathek proxy function: `searchMediathek(query, limit?)` calling GET /api/mediathek/search
- [x] 4.4 Add TypeScript types for write request bodies, test request/response, and mediathek search response

## 5. RuleSet List Search

- [x] 5.1 Add search input to RuleSetList.vue — text field above card grid, client-side filter across ruleSetId, topic, aliases, media IDs (case-insensitive)
- [x] 5.2 Add "New RuleSet" button linking to `/rulesets/new`
- [x] 5.3 Show "No matching rulesets" when search yields no results (distinct from empty state)

## 6. RuleSet Detail Page Actions

- [x] 6.1 Add "Edit" button to RuleSetDetail.vue linking to `/rulesets/:id/edit`
- [x] 6.2 Add "Delete Local" button (visible only when source.localPath is set) with confirmation dialog, calling deleteRuleSet API

## 7. RuleSet Builder UI

- [x] 7.1 Create `RuleSetBuilder.vue` view component with route registration for `/rulesets/new` and `/rulesets/:id/edit`
- [x] 7.2 Implement Identity section: ruleSetId (editable only on create), topic, aliases (dynamic add/remove list), media IDs (tvdbId, imdbId, tmdbId)
- [x] 7.3 Implement default confidence input at ruleset level
- [x] 7.4 Implement Rules section: add/remove rules, collapsible rule cards with ID + strategy summary
- [x] 7.5 Implement strategy picker dropdown with conditional parameter fields per strategy
- [x] 7.6 Implement RegexCapture parameters: seasonRegex, episodeRegex, captureGroup fields (shown for seasonAndEpisodeNumber and byAbsoluteEpisodeNumber)
- [x] 7.7 Implement title rules builder (for itemTitleExact/itemTitleIncludes): ordered list of static/regex parts with add/remove
- [x] 7.8 Implement filter builder: ALL/ANY/NOT sections with add/remove conditions (field dropdown, op dropdown, value input)
- [x] 7.9 Implement Save button: serialize form to RawRuleSet JSON, call createRuleSet or updateRuleSet, navigate to detail on success
- [x] 7.10 Implement edit mode: fetch existing ruleset on mount for `/rulesets/:id/edit`, populate form, ruleSetId read-only
- [x] 7.11 Add breadcrumb navigation: "RuleSets > New" or "RuleSets > {id} > Edit"

## 8. Live Debugger UI

- [x] 8.1 Create `DebuggerPanel.vue` component integrated into the builder page right pane
- [x] 8.2 Implement manual candidate input: form with title, topic, channel, duration (minutes→seconds), quality, description, timestamp fields; add to candidate list
- [x] 8.3 Implement fetch mode: search input calling searchMediathek, selectable candidate list with select all toggle
- [x] 8.4 Implement Test button: serialize builder state as config + candidates, call testRuleSet, display loading state
- [x] 8.5 Implement results display: candidate cards sorted matched-first, green/gray badges, score and matched rule ID
- [x] 8.6 Implement expandable rule pipeline trace: per-rule outcome badges (Matched/FilterFailed/IdentificationFailed/Skipped)
- [x] 8.7 Implement filter trace detail: field, op, expected, actual, pass/fail indicator per condition; skipped conditions in gray
- [x] 8.8 Implement identification trace detail: strategy name, extracted values (season/episode/title), failure reason

## 9. Router & Navigation

- [x] 9.1 Add routes for `/rulesets/new` and `/rulesets/:id/edit` in Vue Router config
- [x] 9.2 Verify sidebar navigation works with new routes and breadcrumbs render correctly
