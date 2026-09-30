## 1. Shared TypeScript types

- [x] 1.1 Create `src/FunkArr.UI/src/types.ts` with all shared interfaces: Rule, Filter, FilterGroup, FilterNode, TitleRule, RuleSetFile, MediaReference, RulesetSummary, MatchedTrace, FilteredTrace, UnmatchedTrace, RuleFailure, MatchRecord, TopicStats, UnmatchedItem, UnmatchedGroup, TestResult, QueueItem, HistoryItem, StatusResponse
- [x] 1.2 Add `channels?: string[]` to RuleSetFile interface and `channels?: string[]` to RulesetSummary
- [x] 1.3 Add `timestamp` to FilterEditor field list, remove any `topicTitle` references

## 2. Fix routing — topic string to tvdbId

- [x] 2.1 Update `router.ts`: change `/rulesets/:topic` to `/rulesets/:tvdbId`, `/rulesets/:topic/edit` to `/rulesets/:tvdbId/edit`
- [x] 2.2 Update `RulesetsView.vue`: navigate to `/rulesets/${summary.media.tvdbId}`, import types from `@/types`
- [x] 2.3 Update `RulesetDetail.vue`: read `tvdbId` from route param, use in API calls (`/rulesets/${tvdbId}`), import types from `@/types`
- [x] 2.4 Update `RulesetEditor.vue`: read `tvdbId` from route param, use in API calls for GET/PUT, import types from `@/types`

## 3. Fix MatchTestPanel endpoint

- [x] 3.1 Update `MatchTestPanel.vue`: POST to `/rulesets/${tvdbId}/test` instead of `/rulesets/test`, accept `tvdbId` as prop, import types from `@/types`

## 4. Add channels to RulesetDetail and RulesetEditor

- [x] 4.1 Update `RulesetDetail.vue`: display `channels` as badges below the topic heading
- [x] 4.2 Update `RulesetEditor.vue`: add channels input field (comma-separated), parse to string[] on save, display from loaded ruleset

## 5. Remove topicTitle from editors

- [x] 5.1 Update `FilterEditor.vue`: ensure field options are `duration, title, description, topic, channel, timestamp` — no `topicTitle`, import types from `@/types`
- [x] 5.2 Update `TitleRuleEditor.vue`: ensure field options are `title, topic, description` — no `topicTitle`, import types from `@/types`

## 6. Update remaining components to shared types

- [x] 6.1 Update `RuleCard.vue`: import types from `@/types`, remove inline interface definitions
- [x] 6.2 Update `MatchesView.vue`: import types from `@/types`, remove inline interface definitions
- [x] 6.3 Update `QueueView.vue`: import `QueueItem` from `@/types`, remove inline interface
- [x] 6.4 Update `HistoryView.vue`: import `HistoryItem` from `@/types`, remove inline interface
- [x] 6.5 Update `SettingsView.vue`: import `StatusResponse` from `@/types`, remove inline interface

## 7. Implement Match Intelligence API endpoints

- [x] 7.1 Implement `GET /matches/recent` in `MatchIntelligenceController`: ShowActor doesn't store match records — endpoint remains stub returning empty array (needs ShowActor persistence change in a future change)
- [x] 7.2 Implement `GET /matches/unmatched` in `MatchIntelligenceController`: ShowActor doesn't store unmatched items — endpoint remains stub (needs ShowActor persistence change in a future change)

## 8. Add auto-generation UI

- [x] 8.1 Add "Generate Rules" button to `RulesetDetail.vue` — calls `POST /api/v1/generate/preview` with tvdbId, shows preview in a modal/section
- [x] 8.2 Add "Apply" button on the preview — calls `POST /api/v1/generate/apply`, reloads detail

## 9. Verify

- [x] 9.1 Backend builds: `dotnet build FunkArr.slnx` from `src/`
- [x] 9.2 Frontend builds: `npm run build` from `src/FunkArr.UI/`
- [x] 9.3 Backend tests pass: `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` from `src/`
- [x] 9.4 Run `dotnet format` on changed .cs files
