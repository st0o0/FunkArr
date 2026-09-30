## Why

Skip/filter keywords for accessibility variants and content types (Trailer, Teaser, etc.) are defined independently in three places: `RuleSetMatchingEngine`, `MatchingPipeline`, and `RuleSetGeneratorActor`. The lists have already drifted — each has different keywords — and adding a new keyword requires knowing which of the three locations to update.

## What Changes

- Introduce a shared `ContentFilter` static class in `Shared/` that defines all skip keywords in one place
- Replace the three independent keyword lists and `ShouldSkip`/`ShouldSkipAccessibility`/`IsAccessibilityVariant` methods with calls to `ContentFilter`
- Distinguish between two filter categories: accessibility keywords (always applied) and content-type keywords (only applied in the generic pipeline)

## Capabilities

### New Capabilities
- `content-filter`: Centralized content filtering with two keyword categories — accessibility variants (Audiodeskription, Gebärdensprache, etc.) and content-type skip keywords (Trailer, Vorschau, Teaser). Provides `IsAccessibilityVariant(title)`, `ShouldSkip(title, topic)`, and `ShouldSkipAccessibilityOnly(title)` methods.

### Modified Capabilities

## Impact

- `FunkArr.RuleSet.RuleSetMatchingEngine` — remove `AccessibilityKeywords` array and `ShouldSkipAccessibility`, delegate to `ContentFilter`
- `FunkArr.Search.MatchingPipeline` — remove `SkipKeywords` array and `ShouldSkip`, delegate to `ContentFilter`
- `FunkArr.RuleSet.RuleSetGeneratorActor` — remove `IsAccessibilityVariant`, delegate to `ContentFilter`
- Existing tests updated to verify unified behavior
