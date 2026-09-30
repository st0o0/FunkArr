## Why

The Match Detail View currently shows only a shallow summary per item: the title, the winning rule index, strategy name, and confidence. It hides the raw Mediathek data (duration, channel, topic), the full rule evaluation pipeline (which rules were tried, why each failed), and regex capture details (what pattern was applied, what was extracted). Users who want to improve RuleSets cannot diagnose matching behavior from the current view — they have to mentally replay the engine logic. The fragmented trace model (three separate types: MatchedTrace, FilteredTrace, UnmatchedTrace) with string-based discrimination makes the API awkward and the frontend types incomplete. Replacing this with a unified, enum-driven evaluation model gives the UI the data it needs and simplifies the entire trace pipeline.

## What Changes

- **BREAKING**: Replace `MatchRecord` with `SearchEvaluation` containing a flat `ItemEvaluation[]` list instead of three separate trace lists
- **BREAKING**: Replace `MatchedTrace`, `FilteredTrace`, `UnmatchedTrace` with unified `ItemEvaluation` record discriminated by `EvaluationOutcome` enum (int-backed)
- Add `RuleEvaluation` record capturing per-rule filter checks and strategy detail for every rule tested per item (not just the winner)
- Add `EvaluationOutcome` enum (Matched=0, Filtered=1, Unmatched=2) and `RuleOutcome` enum (Matched=0, FilterFailed=1, NoMatch=2), both int-backed
- Add `FilterCheck` record (field, op, value, actual, passed) for filter evaluation transparency
- Add `StrategyDetail` record (regexPattern, regexInput, regexMatched, capturedValue, constructedTitle, tvdbMatch) for strategy evaluation transparency
- Extend `RuleSetMatchingEngine.EvaluateRulesWithTraces()` and `EvaluateMovieRulesWithTraces()` to collect full pipeline detail per item
- **BREAKING**: Update API contracts and TypeScript types to match the new model
- Redesign `MatchDetailView.vue` with expandable pipeline rows showing raw item data and full evaluation path per item
- Expose `itemTopic`, `itemDuration`, `itemChannel` in TypeScript types (already in API contracts but missing in frontend)

## Capabilities

### New Capabilities

- `search-evaluation-model`: Unified SearchEvaluation/ItemEvaluation/RuleEvaluation data model with int-backed enums replacing the fragmented MatchRecord/MatchedTrace/FilteredTrace/UnmatchedTrace types

### Modified Capabilities

- `ruleset-matching-engine`: EvaluateRulesWithTraces and EvaluateMovieRulesWithTraces must emit RuleEvaluation[] with FilterCheck and StrategyDetail per rule per item
- `match-intelligence-api`: Endpoints return SearchEvaluation with ItemEvaluation[] instead of MatchRecord with three separate trace lists
- `match-detail-view`: Redesigned to show raw item data, expandable evaluation pipeline per item, regex capture details, and filter check results
- `api-contracts`: New contract types for SearchEvaluation, ItemEvaluation, RuleEvaluation, FilterCheck, StrategyDetail and int-backed enums
- `match-views`: MatchesView must adapt to the new SearchEvaluation shape (summary counts derived from items instead of separate lists)
- `recent-match-actor`: Stores SearchEvaluation instead of MatchRecord
- `contract-tests`: Wire-format snapshots must be updated for the new trace shape

## Impact

- **Backend**: `MatchTrace.cs` (replaced), `RuleSetMatchingEngine.cs` (enriched trace collection), `RecentMatchActor` (new storage type), `ShowActor`/`MovieActor` (adapted match quality recording), `ContractMappingExtensions.cs` (new mappings), generated API contracts
- **Frontend**: `types.ts` (new interfaces + enums), `MatchDetailView.vue` (full redesign), `MatchesView.vue` (adapted to new shape)
- **Tests**: Contract snapshot tests, matching engine tests for new trace data
- **Storage**: SearchEvaluation records are ~3-5x larger than MatchRecord due to full pipeline data; RecentMatchActor ring buffer limit may need tuning
