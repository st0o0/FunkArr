## Why

The matching architecture has accumulated significant technical debt: MatchingPipeline is a grab-bag of concerns with dead code paths, quality expansion is duplicated four times across actors, ContentFilter has a confusing two-mode split, and the auto-generator bypasses rate limiting with a separate Mediathek API call when TvSearchActor already has the items. The `topicTitle` composite field is a workaround that can be eliminated with proper field separation. This change rebuilds matching into focused, single-responsibility components and makes RuleSetMatchingEngine the sole TV matching path with auto-generation as the fallback.

## What Changes

- **BREAKING** Delete `MatchingPipeline.cs` entirely — dissolve into `QualityExpander`, `ResultScorer`, `ShowMatcher`
- **BREAKING** Delete `RuleSetGeneratorActor.cs` — replace with static `RuleSetGenerator` class (no separate actor, no Mediathek API call)
- **BREAKING** Remove `topicTitle` composite field from `RuleSetMatchingEngine.GetFieldValue()` — rules use actual API fields (`topic`, `title`, `description`, `channel`)
- **BREAKING** Remove `ContentFilter.ShouldSkipAccessibilityOnly` — single `ShouldSkip` method for all paths
- **BREAKING** Remove the no-rules heuristic fallback in `TvSearchActor` — auto-generation fills the gap, first request for unknown show returns empty
- Add `Channels` property (string[]) to `RuleSetFile` — community rulesets set explicitly, auto-generator derives from items
- Extend `RulesResponse` with `Channels` — TvSearchActor uses for `.FromChannel()` in query
- Add `GenerateFromItems` message on `RuleSetActor` — TvSearchActor forwards fetched items for auto-generation
- Auto-generator derives channels and duration from provided items
- TvSearchActor derives minimum duration from rules for `.WithDuration()` in query
- Update all community rulesets: remove `topicTitle` references, add `channels` where known
- Absorb remaining tasks from `matching-engine-mediathek-alignment` change

## Capabilities

### New Capabilities

- `quality-expander`: Single component for expanding MediathekResultItems into multi-quality SearchResults using UrlPatternAnalyzer and size estimation
- `result-scorer`: Scoring logic for SearchResults (quality tier, topic name match, air date proximity)

### Modified Capabilities

- `ruleset-matching-engine`: Remove `topicTitle` composite field, use unified ContentFilter (accessibility + trailers), all rules reference actual API fields
- `ruleset-auto-generation`: Static generator class (no actor), receives items instead of sampling Mediathek, derives channels from items, always generates `field: "title"` for TitleRules
- `tv-search-pipeline`: Single matching path (RuleSetMatchingEngine only), auto-gen trigger via forwarded items, channel and duration from RuleSet in query, uses shared QualityExpander and ResultScorer
- `text-search-pipeline`: Uses shared QualityExpander and ResultScorer instead of local duplicates
- `movie-search-pipeline`: Uses shared QualityExpander and ResultScorer instead of local duplicates
- `content-filter`: Remove dual-mode split, single ShouldSkip method used by all paths
- `community-dataset`: RuleSetFile gains `channels` property, rules updated to remove `topicTitle` references

## Impact

- **Source files deleted**: `MatchingPipeline.cs`, `RuleSetGeneratorActor.cs`
- **Source files added**: `QualityExpander.cs`, `ResultScorer.cs`, `ShowMatcher.cs`, `RuleSetGenerator.cs`
- **Source files modified**: `RuleSetModels.cs`, `RuleSetActor.cs`, `RuleSetMatchingEngine.cs`, `ContentFilter.cs`, `TvSearchActor.cs`, `TextSearchActor.cs`, `MovieSearchActor.cs`, `BrowseActor.cs`
- **Data files**: All community rulesets using `topicTitle` updated, `channels` added where known
- **Tests**: Generator tests migrated to static class, MatchingPipeline tests deleted/migrated, new component tests
- **Old change absorbed**: `matching-engine-mediathek-alignment` (tasks 5-6 become obsolete, `topicTitle` reversed)
