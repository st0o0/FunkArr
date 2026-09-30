## 1. Backend Bug Fixes

- [x] 1.1 Add `TmdbId` (int?) and `Type` (string?) fields to `GenerateApplyRequest` record in `GenerateController.cs`
- [x] 1.2 Rewrite `Apply` method to use explicit `Type` field for MediaType with fallback inference from ID fields; use TmdbId as entity key for movies when ImdbId is absent
- [x] 1.3 Add try-catch with Problem Details responses to all `MatchIntelligenceController` endpoints (GetRecent, GetAllTopicStats, GetTopicStats, GetUnmatched)
- [x] 1.4 Write contract tests for the updated `GenerateApplyRequest` (TmdbId field, type inference)

## 2. Backend — Ad-hoc Test Endpoint

- [x] 2.1 Add `POST /api/v1/rulesets/test-adhoc` endpoint to `RulesetController` that accepts `{ ruleSet, query }`, queries MediathekViewWeb, evaluates rules with traces, and returns `ItemEvaluation[]`
- [x] 2.2 Write tests for the ad-hoc test endpoint (empty results, no rules, normal evaluation)

## 3. Frontend — GeneratePreviewPanel Component

- [x] 3.1 Create `GeneratePreviewPanel.vue` component that renders generated rules via `RuleCard`, test trace summary (matched/filtered/unmatched counts), and confidence score
- [x] 3.2 Add Accept, Edit, and Discard action buttons to the panel with corresponding emit events
- [x] 3.3 Wire Accept to POST `/generate/apply` with the generated RuleSet and correct type/ID, then reload the detail page

## 4. Frontend — Universal Generate on RulesetDetail

- [x] 4.1 Remove the `v-if="ruleset.source === 'generated'"` condition from the Generate button in `RulesetDetail.vue`
- [x] 4.2 Replace the `generateRules()` function: call preview, store result in a ref, show `GeneratePreviewPanel` inline instead of auto-applying
- [x] 4.3 Pass correct `type` and entity ID (TvdbId for shows, TmdbId for movies) to both preview and apply calls
- [x] 4.4 Handle the Edit action: navigate to RulesetEditor with generated rules pre-populated via route state or query params

## 5. Frontend — Guided Ruleset Creation Wizard

- [x] 5.1 Create `RulesetCreationWizard.vue` with three-step layout (Search → Connect → Preview) and step indicator
- [x] 5.2 Implement Step 1: text input + type toggle (show/movie), search button calling `POST /generate/preview` with query, display Mediathek item count and detected channels
- [x] 5.3 Implement Step 2: render TVDB/TMDB candidates from preview response, allow selection or manual ID entry, re-run preview with explicit ID
- [x] 5.4 Implement Step 3: embed `GeneratePreviewPanel`, wire Save (apply + navigate to detail), Edit (navigate to editor with pre-fill), and Discard (reset wizard)
- [x] 5.5 Add "Create Manually" link at each step that navigates to RulesetEditor with empty form
- [x] 5.6 Update router: change `/rulesets/new` to render `RulesetCreationWizard` instead of `RulesetEditor`

## 6. Frontend — Setup Status Banner

- [x] 6.1 Add a dismissible banner component to `App.vue` below the header, driven by the existing `status` polling ref
- [x] 6.2 Derive banner text from status fields: prioritize `configured === false` and `ffmpeg.found === false` (red), then paths/mediathek issues (amber)
- [x] 6.3 Add dismiss button that sets a session-scoped ref to hide the banner until page reload
- [x] 6.4 Hide banner when `route.path === '/setup'` or when all checks pass

## 7. Verification

- [x] 7.1 Run `dotnet build FunkArr.slnx` and `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` — all tests pass
- [x] 7.2 Start dev server, verify Generate button appears on community and local ruleset detail pages
- [x] 7.3 Verify generate preview shows results before applying, and Accept/Discard work correctly
- [x] 7.4 Verify new ruleset wizard: search → connect → preview → save flow
- [x] 7.5 Verify setup banner appears when FFmpeg is missing or paths are not writable, and dismiss works
- [x] 7.6 Verify `/api/v1/matches/recent` returns structured error instead of empty 500 when actor is unavailable
- [x] 7.7 Run `dotnet format` on changed .cs files
