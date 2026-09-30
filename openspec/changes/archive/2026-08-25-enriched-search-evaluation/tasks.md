## 1. Data Model

- [x] 1.1 Add `EvaluationOutcome` enum (Matched=0, Filtered=1, Unmatched=2) and `RuleOutcome` enum (Matched=0, FilterFailed=1, NoMatch=2) to `RuleSet/` namespace
- [x] 1.2 Add `FilterCheck` record (Field, Op as FilterOp, Value, Actual, Passed)
- [x] 1.3 Add `StrategyDetail` record (RegexPattern?, RegexInput?, RegexMatched?, CapturedValue?, ConstructedTitle?, TvdbMatch?)
- [x] 1.4 Add `RuleEvaluation` record (RuleIndex, Priority, Strategy as MatchingStrategy, Outcome as RuleOutcome, FilterChecks, StrategyDetail?)
- [x] 1.5 Add `ItemEvaluation` record (ItemTitle, ItemTopic, ItemChannel, ItemDuration, Outcome as EvaluationOutcome, Season?, Episode?, EpisodeName?, Confidence?, WinnerRuleIndex?, FilterReason?, RuleEvaluations)
- [x] 1.6 Add `SearchEvaluation` record (Id, Timestamp, SearchTopic, TvdbId?, Season?, Episode?, Source, TotalResults, Items as IReadOnlyList<ItemEvaluation>)
- [x] 1.7 Remove `MatchedTrace`, `FilteredTrace`, `UnmatchedTrace`, `RuleFailure`, `MatchRecord` from `MatchTrace.cs` (keep `TopicStats`)

## 2. Matching Engine

- [x] 2.1 Add `EvaluateFilterGroupWithChecks` method that returns `List<FilterCheck>` for all filters in a FilterGroup
- [x] 2.2 Refactor `MatchSeasonAndEpisode` to return `(MatchedEpisodeInfo?, StrategyDetail)` tuple
- [x] 2.3 Refactor `MatchAbsoluteEpisode` to return `(MatchedEpisodeInfo?, StrategyDetail)` tuple
- [x] 2.4 Refactor `MatchTitleExact` to return `(MatchedEpisodeInfo?, StrategyDetail)` tuple
- [x] 2.5 Refactor `MatchTitleIncludes` to return `(MatchedEpisodeInfo?, StrategyDetail)` tuple
- [x] 2.6 Refactor `MatchAirdate` to return `(MatchedEpisodeInfo?, StrategyDetail)` tuple
- [x] 2.7 Refactor `MatchMovieTitle` to return `(bool, StrategyDetail)` tuple
- [x] 2.8 Rewrite `EvaluateRulesWithTraces` to collect `List<RuleEvaluation>` per item and return `(IReadOnlyList<MatchedEpisodeInfo>, IReadOnlyList<ItemEvaluation>)`
- [x] 2.9 Rewrite `EvaluateMovieRulesWithTraces` to same pattern

## 3. Actors

- [x] 3.1 Update `ShowActor.HandleMatch` to build `SearchEvaluation` from `ItemEvaluation[]` and send to `RecentMatchActor`
- [x] 3.2 Update `MovieActor.HandleMatch` to same pattern
- [x] 3.3 Update `ShowActorState`/`MovieActorState` match quality tracking to derive counts from `ItemEvaluation.Outcome`
- [x] 3.4 Update `RecentMatchActor` message types: `RecordSearchEvaluation(SearchEvaluation)` replacing `RecordMatch(MatchRecord)`
- [x] 3.5 Update `RecentMatchActor` persistence events: `SearchEvaluationAdded` replacing `MatchRecordAdded`
- [x] 3.6 Update `RecentMatchActor` state to store `SearchEvaluation` in ring buffer
- [x] 3.7 Update `RecentMatchActor` unmatched aggregation to derive from `ItemEvaluation` with Outcome=Unmatched

## 4. API Contracts

- [x] 4.1 Update `openapi/match-intelligence.yaml` with new SearchEvaluation, ItemEvaluation, RuleEvaluation, FilterCheck, StrategyDetail schemas and int-backed enums
- [x] 4.2 Regenerate `Api/Generated/Contracts.g.cs` via NSwag
- [x] 4.3 Update `ContractMappingExtensions` with `ToContract()` for SearchEvaluation, ItemEvaluation, RuleEvaluation, FilterCheck, StrategyDetail
- [x] 4.4 Update `MatchIntelligenceController` to return mapped SearchEvaluation contracts
- [x] 4.5 Configure NSwag/System.Text.Json to serialize all enums as integers

## 5. Frontend Types and Views

- [x] 5.1 Update `types.ts`: add EvaluationOutcome, RuleOutcome enums (numeric), SearchEvaluation, ItemEvaluation, RuleEvaluation, FilterCheck, StrategyDetail interfaces; remove MatchedTrace, FilteredTrace, UnmatchedTrace, RuleFailure, MatchRecord
- [x] 5.2 Update `MatchesView.vue` to use SearchEvaluation shape (derive counts from items array)
- [x] 5.3 Redesign `MatchDetailView.vue`: raw item data on compact rows, expandable pipeline visualization per item
- [x] 5.4 Add pipeline stepper component showing RuleEvaluation chain with filter checks and strategy detail
- [x] 5.5 Add filter check visualization (field, op, value, actual, pass/fail indicator)
- [x] 5.6 Add strategy detail visualization (regex pattern/input/capture, constructed title, TVDB match)

## 6. Tests

- [x] 6.1 Update matching engine tests to verify `ItemEvaluation[]` output with RuleEvaluation pipeline
- [x] 6.2 Add test: matched item contains RuleEvaluations for all rules tried (not just winner)
- [x] 6.3 Add test: FilterCheck captures all individual filters with actual values
- [x] 6.4 Add test: StrategyDetail captures regex pattern, input, and captured value
- [x] 6.5 Update `RecentMatchActor` persistence journal snapshot tests for `SearchEvaluationAdded`
- [x] 6.6 Update contract mapping tests for new types
- [x] 6.7 Run `dotnet format` and verify build passes

## 7. Cleanup

- [x] 7.1 Remove old `MatchedTraceContract`, `FilteredTraceContract`, `UnmatchedTraceContract`, `MatchSummary` from OpenAPI spec and generated contracts
- [x] 7.2 Remove old `MatchedTraceContractStrategy` string enum from generated contracts
- [x] 7.3 Verify all references to old types are removed (grep for MatchRecord, MatchedTrace, FilteredTrace, UnmatchedTrace)
