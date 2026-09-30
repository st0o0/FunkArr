## Why

Two backend changes (mediathek-query-full-integration and matching-engine-rebuild) modified the RuleSetFile model, removed the `topicTitle` composite field, added channels, and restructured the matching architecture. The frontend was not updated alongside these changes. Additionally, pre-existing routing mismatches between the Vue frontend (using topic strings) and the API controllers (expecting tvdbId integers) mean ruleset CRUD is broken. The MatchesView has non-functional sub-views (recent endpoint missing, unmatched is a stub). TypeScript interfaces are duplicated across every Vue file with no shared types.

## What Changes

- **Extract shared TypeScript types** into `src/FunkArr.UI/src/types.ts` — all Vue files import from there
- **Add `channels`** to TS `RuleSetFile` interface, display in RulesetDetail, editable in RulesetEditor
- **Remove `topicTitle`** from FilterEditor and TitleRuleEditor field lists
- **Fix routing mismatch** — frontend routes and API calls use `tvdbId` (int) instead of `topic` (string) for show rulesets
- **Fix MatchTestPanel** — POST to `/{tvdbId}/test` instead of `/test`
- **Implement `GET /matches/recent`** in MatchIntelligenceController — query MatchQualityActor for recent records
- **Implement `GET /matches/unmatched`** in MatchIntelligenceController — aggregate unmatched items from MatchQualityActor
- **Add auto-generation UI** — integrate GenerateController preview/apply endpoints into RulesetDetail

## Capabilities

### New Capabilities

_(none — all changes extend existing capabilities)_

### Modified Capabilities

- `match-intelligence-api`: Implement recent matches and unmatched items endpoints (currently missing/stub)
- `match-views`: All three sub-views functional (recent, topics, unmatched), shared types
- `ruleset-builder`: Add channels editor, remove topicTitle from filter/title-rule field lists, fix routing to use tvdbId, fix test endpoint
- `ruleset-api`: Frontend uses tvdbId routing matching controller expectations

## Impact

- **Frontend files modified**: types.ts (new), router.ts, RulesetsView.vue, RulesetDetail.vue, RulesetEditor.vue, MatchesView.vue, FilterEditor.vue, TitleRuleEditor.vue, MatchTestPanel.vue, RuleCard.vue
- **Backend files modified**: MatchIntelligenceController.cs (implement /recent and /unmatched)
- **No backend API contract changes** — the controller routes already exist correctly, the frontend was calling them wrong
