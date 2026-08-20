# Tasks: shared-prefilter

## 1. Shared ContentFilter

- [ ] 1.1 Create `src/FunkArr/Shared/ContentFilter.cs` with a `public static
      class ContentFilter` in namespace `FunkArr.Shared`
- [ ] 1.2 Define `private static readonly string[] AccessibilityKeywords` =
      `Audiodeskription`, `Gebärdensprache`, `Gebardensprache`,
      `klare Sprache`, `Hörfassung` (union of all three existing lists)
- [ ] 1.3 Define `private static readonly string[] ContentTypeKeywords` =
      `Trailer`, `Vorschau`, `Teaser`
- [ ] 1.4 Implement `public static bool IsAccessibilityVariant(string
      title)` — true if `title` contains any `AccessibilityKeywords`
      (`StringComparison.OrdinalIgnoreCase`)
- [ ] 1.5 Implement `public static bool ShouldSkipAccessibilityOnly(string
      title)` as an alias/equivalent of `IsAccessibilityVariant` for the
      ruleset-engine call site's naming
- [ ] 1.6 Implement `public static bool ShouldSkip(string title, string
      topic)` — true if `title` contains any `AccessibilityKeywords` or
      `ContentTypeKeywords`, or `topic` contains any `ContentTypeKeywords`
      (`StringComparison.OrdinalIgnoreCase`)

## 2. Update RuleSetMatchingEngine

- [ ] 2.1 Remove `AccessibilityKeywords` array from
      `src/FunkArr/RuleSet/RuleSetMatchingEngine.cs`
- [ ] 2.2 Remove `ShouldSkipAccessibility` method
- [ ] 2.3 Replace both call sites (`EvaluateRules`,
      `EvaluateRulesWithTraces`) with
      `ContentFilter.ShouldSkipAccessibilityOnly(item.Title)`
- [ ] 2.4 Add `using FunkArr.Shared;` if not already present

## 3. Update MatchingPipeline

- [ ] 3.1 Remove `SkipKeywords` array from
      `src/FunkArr/Search/MatchingPipeline.cs`
- [ ] 3.2 Remove the private `ShouldSkip(MediathekResultItem item)` method
- [ ] 3.3 Replace call sites (`Execute`, `ExecuteAsync`, `FilterResults`)
      with `ContentFilter.ShouldSkip(item.Title, item.Topic)`
- [ ] 3.4 Add `using FunkArr.Shared;` if not already present

## 4. Update RuleSetGeneratorActor

- [ ] 4.1 Remove `IsAccessibilityVariant` method from
      `src/FunkArr/RuleSet/RuleSetGeneratorActor.cs`
- [ ] 4.2 Replace call site(s) with `ContentFilter.IsAccessibilityVariant`
- [ ] 4.3 Add `using FunkArr.Shared;` if not already present

## 5. Tests

- [ ] 5.1 Add `src/FunkArr.Tests/Shared/ContentFilterTests.cs` covering:
      accessibility keyword detection (all five keywords), content-type
      keyword detection (all three keywords), combined `ShouldSkip` against
      title and topic independently, `ShouldSkipAccessibilityOnly` ignoring
      content-type keywords and topic, case-insensitivity, and negative
      cases (no keyword present)
- [ ] 5.2 Update `src/FunkArr.Tests/RuleSet/RuleSetMatchingEngineTests.cs`
      if it references the removed `AccessibilityKeywords` array or
      `ShouldSkipAccessibility` method directly
- [ ] 5.3 Update `src/FunkArr.Tests/Search/MatchingPipelineTests.cs` if it
      references the removed `SkipKeywords` array or `ShouldSkip` method
      directly, and add cases for the two newly-recognized keywords
      (`Gebardensprache` without umlaut, `klare Sprache`) now skipped by
      the generic pipeline
- [ ] 5.4 Update `src/FunkArr.Tests/RuleSet/RuleSetGeneratorTests.cs` if it
      references the removed `IsAccessibilityVariant` method directly, and
      add a case confirming `Hörfassung` is now recognized as an
      accessibility variant

## 6. Verification

- [ ] 6.1 `dotnet build FunkArr.slnx` from `src/` passes with no warnings
- [ ] 6.2 `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` passes
      (all tests, including new `ContentFilterTests`)
- [ ] 6.3 Confirm no remaining references to the removed keyword arrays or
      methods (`AccessibilityKeywords`, `SkipKeywords`,
      `ShouldSkipAccessibility` in `RuleSetMatchingEngine`, `ShouldSkip` in
      `MatchingPipeline`, `IsAccessibilityVariant` in
      `RuleSetGeneratorActor`) outside of `ContentFilter` itself
