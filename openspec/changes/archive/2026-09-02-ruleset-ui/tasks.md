## 1. Query Messages

- [x] 1.1 Add `QueryRegisteredRuleSets` and `RegisteredRuleSetsResult` (with `RegisteredRuleSetEntry`) records to `FunkArr.Messages/RuleSet/`
- [x] 1.2 Add `QueryRuleSetDetail` and `RuleSetDetailResult` (with `RuleSetDetailRule`) records to `FunkArr.Messages/RuleSet/`

## 2. Actor Query Handlers

- [x] 2.1 Add `Receive<QueryRegisteredRuleSets>` handler to `RuleSetResolver` — build result from existing `TopicByRuleSetId`, `EntriesByRuleSetId`, and `IdIndex` state
- [x] 2.2 Add `QueryRegisteredRuleSets` handling logic as extension method on `RuleSetResolverState`
- [x] 2.3 Add `Receive<QueryRuleSetDetail>` handler to `RuleSetManager` — re-read JSON files via `RuleSetMerger.ExtractIdentity()` + `RuleSetMerger.Build()`, combine with source metadata from `KnownRuleSets`
- [x] 2.4 Add tests for `RuleSetResolverState.QueryAll` (empty, single, multiple rulesets)
- [x] 2.5 Add tests for `RuleSetManager` detail query (known ruleset, unknown ruleset, deleted file)

## 3. REST API Endpoints

- [x] 3.1 Add `RuleSetApiEndpoints` class in `FunkArr.Api` with `MapRuleSetApi()` extension method
- [x] 3.2 Implement `GET /api/rulesets` — Ask RuleSetResolver with `QueryRegisteredRuleSets`, map to JSON
- [x] 3.3 Implement `GET /api/rulesets/{id}` — Ask RuleSetManager with `QueryRuleSetDetail`, map to JSON or 404
- [x] 3.4 Implement `GET /api/rulesets/{id}/history` — Ask MatchHistoryWorker shard region with `QueryScoringHistory`, map to JSON
- [x] 3.5 Implement `GET /api/rulesets/{id}/history/{requestId}` — Ask MatchHistoryWorker with `QueryScoringDetail`, map to JSON or 404
- [x] 3.6 Add API endpoint tests

## 4. Static File Serving & Host Wiring

- [x] 4.1 Add `app.UseStaticFiles()` in `ApplicationSetupContainer` before endpoint mapping
- [x] 4.2 Add `app.MapRuleSetApi()` call in `ApplicationSetupContainer`
- [x] 4.3 Add SPA fallback route (`MapFallbackToFile("index.html")`) as last route
- [x] 4.4 Configure Vite build output path so dist/ is available as static web assets to the host project

## 5. Vue UI — Layout & Dashboard

- [x] 5.1 Install any needed dependencies (e.g. date formatting utility if needed)
- [x] 5.2 Create app layout component with sidebar/header navigation (Dashboard, RuleSets links)
- [x] 5.3 Update `App.vue` to use layout component
- [x] 5.4 Update Dashboard page (`/`) with FunkArr branding and navigation to rulesets

## 6. Vue UI — RuleSet List

- [x] 6.1 Create API client module (`src/api/rulesets.ts`) with typed fetch wrappers for all 4 endpoints
- [x] 6.2 Create RuleSet list page at `/rulesets` — fetch and display ruleset cards with identity info
- [x] 6.3 Add loading, empty, and error states
- [x] 6.4 Add router entry for `/rulesets`

## 7. Vue UI — RuleSet Detail

- [x] 7.1 Create RuleSet detail page at `/rulesets/:id` with Identity, Source, and Matching Rules sections
- [x] 7.2 Render identity section (topic, aliases, media IDs)
- [x] 7.3 Render source section (community/local paths, timestamps, merge mode)
- [x] 7.4 Render matching rules section (default confidence, rule cards with strategy, filters, patterns)
- [x] 7.5 Add link to scoring history
- [x] 7.6 Add router entry for `/rulesets/:id`

## 8. Vue UI — Scoring History & Detail

- [x] 8.1 Create scoring history page at `/rulesets/:id/history` — table of past runs with pagination
- [x] 8.2 Create scoring detail page at `/rulesets/:id/history/:requestId` — item traces with expandable rule traces
- [x] 8.3 Add router entries for history and detail pages

## 9. Integration & Polish

- [x] 9.1 Build Vue frontend (`npm run build`) and verify static files are served by the .NET host
- [x] 9.2 Test full flow: start service, navigate to dashboard → rulesets → detail → history → scoring detail
- [x] 9.3 Verify SPA fallback works (direct navigation to `/rulesets/tatort` serves index.html)
- [x] 9.4 Run `dotnet format` and fix any violations
- [x] 9.5 Run all existing tests to verify no regressions
