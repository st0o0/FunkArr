## 1. Extract shared components from MatchingPipeline

- [x] 1.1 Create `QualityExpander` static class in `Search/Quality/` — extract quality expansion logic with UrlPatternAnalyzer, size estimation, and SearchResult field mapping
- [x] 1.2 Create `ResultScorer` static class in `Search/Matching/` — extract scoring logic (quality tier, topic match, airdate proximity)
- [x] 1.3 Create `ShowMatcher` static class in `Search/Matching/` — extract show name substring matching with TitleNormalizer (umlaut expansion, 13-char prefix fallback)
- [x] 1.4 Write tests for QualityExpander (all-qualities, single-quality, empty-URL, pattern-based quality override, size estimation)
- [x] 1.5 Write tests for ResultScorer (quality scoring, topic match, airdate proximity)
- [x] 1.6 Write tests for ShowMatcher (exact match, normalized match, prefix fallback, no match)

## 2. Unify ContentFilter

- [x] 2.1 Remove `ShouldSkipAccessibilityOnly` and `IsAccessibilityVariant` from `ContentFilter` — single `ShouldSkip(title, topic)` remains
- [x] 2.2 Update `RuleSetMatchingEngine` to use `ContentFilter.ShouldSkip(title, topic)` instead of `ShouldSkipAccessibilityOnly`
- [x] 2.3 Update ContentFilter tests

## 3. Remove topicTitle from RuleSetMatchingEngine

- [x] 3.1 Remove `"topicTitle"` case from `GetFieldValue()` in `RuleSetMatchingEngine`
- [x] 3.2 Update `RuleSetMatchingEngine` tests to remove topicTitle scenarios

## 4. Add channels to RuleSetFile and RulesResponse

- [x] 4.1 Add `Channels` property (IReadOnlyList<string>?) to `RuleSetFile` in `RuleSetModels.cs`
- [x] 4.2 Extend `RulesResponse` in `RuleSetActor` to include `Channels` (IReadOnlyList<string>?)
- [x] 4.3 Update `HandleGetRulesForTopic` to pass channels from the matched RuleSetFile to the response
- [x] 4.4 Add `DeriveMinDuration` static helper — scans rules for duration greaterThan filters, returns min * 60

## 5. Convert RuleSetGeneratorActor to static RuleSetGenerator

- [x] 5.1 Create `RuleSetGenerator` static class — move `FindBestTopic`, `AnalyzePatterns`, `DetectStrategy`, `GenerateRegex`, `DeriveDurationFilter`, `ComputeConfidence` from `RuleSetGeneratorActor`
- [x] 5.2 Add `Generate(MediathekResultItem[] items, int tvdbId, string showName)` method returning `RuleSetFile?`
- [x] 5.3 Add `DeriveChannels` — collects distinct Channel values from items matching the detected topic
- [x] 5.4 Update all GenerateRegex calls to always use `field: "title"` (remove topicTitle logic)
- [x] 5.5 Add `GenerateFromItems` message handler to `RuleSetActor` — calls `RuleSetGenerator.Generate()`, updates indexes, writes to disk
- [x] 5.6 Remove `GenerateRuleSet` message and GeneratorActor spawning from `RuleSetActor`
- [x] 5.7 Delete `RuleSetGeneratorActor.cs`
- [x] 5.8 Update generator tests to test static class methods directly

## 6. Rebuild TvSearchActor matching path

- [x] 6.1 Remove the no-rules fallback path (MatchingPipeline.Execute with MatchContext)
- [x] 6.2 Update `TryAdvanceAfterResolution` — build query with `.FromChannel()` from RulesResponse.Channels and `.WithDuration()` from DeriveMinDuration
- [x] 6.3 Update `OnItemsQueried` — when rules exist, use ContentFilter → RuleSetMatchingEngine → QualityExpander → ResultScorer
- [x] 6.4 Update `OnItemsQueried` — when no rules, return empty results and Tell RuleSetActor with GenerateFromItems
- [x] 6.5 Remove inline quality expansion from `ExecuteRuleSetPath` — use shared QualityExpander
- [x] 6.6 Update TvSearchActor tests

## 7. Rebuild TextSearchActor and MovieSearchActor

- [x] 7.1 Update `TextSearchActor` — replace local ExpandQualities with ContentFilter → QualityExpander → ResultScorer, remove dead MatchingPipeline.Execute call
- [x] 7.2 Update `MovieSearchActor` — replace local ExpandQualities with ContentFilter → ShowMatcher → QualityExpander → ResultScorer
- [x] 7.3 Update `BrowseActor` — use QualityExpander if applicable
- [x] 7.4 Update TextSearchActor, MovieSearchActor, BrowseActor tests

## 8. Delete MatchingPipeline

- [x] 8.1 Delete `MatchingPipeline.cs`
- [x] 8.2 Remove any remaining references to MatchingPipeline across the codebase
- [x] 8.3 Delete or migrate MatchingPipeline tests

## 9. Update community rulesets

- [x] 9.1 Update all community rulesets using `field: "topicTitle"` to use `field: "title"` with adjusted patterns
- [x] 9.2 Add `channels` to community rulesets where channel is known
- [x] 9.3 Validate all rulesets deserialize correctly

## 10. Archive old change and verify

- [x] 10.1 Archive `matching-engine-mediathek-alignment` change (absorbed into this change)
- [x] 10.2 Build passes: `dotnet build FunkArr.slnx` from `src/`
- [x] 10.3 All tests pass: `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` from `src/`
- [x] 10.4 Run `dotnet format` on changed .cs files
