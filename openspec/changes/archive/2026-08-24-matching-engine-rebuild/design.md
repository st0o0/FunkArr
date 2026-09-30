## Context

The matching architecture grew organically: MatchingPipeline was the original heuristic engine, then RuleSetMatchingEngine was added for structured rule-based matching, but both coexist with duplicated concerns. TvSearchActor branches into two completely different paths depending on whether rules exist. Quality expansion logic is copy-pasted across four locations. The auto-generator spawns a child actor that makes its own Mediathek API call, bypassing rate limiting. The `topicTitle` composite field was a workaround for rules that mixed topic and title in regex patterns.

## Goals / Non-Goals

**Goals:**
- Single-responsibility components: QualityExpander, ResultScorer, ShowMatcher, ContentFilter
- RuleSetMatchingEngine as the sole TV matching path (no heuristic fallback)
- Auto-generation using already-fetched items (zero extra API calls)
- Channels on RuleSetFile for precise Mediathek queries
- Remove topicTitle workaround — rules use actual API fields
- Unified ContentFilter (one mode, not two)
- Absorb the incomplete matching-engine-mediathek-alignment change

**Non-Goals:**
- Changes to RuleSetActor index/lookup mechanism
- RefreshActor or MatchQualityActor changes
- Persistence DTO changes
- Override/merge mechanism changes
- RuleSet builder/editor UI

## Decisions

### Decision 1: Dissolve MatchingPipeline into focused components

**Choice:** Delete `MatchingPipeline.cs`. Extract into: `QualityExpander` (quality expansion with UrlPatternAnalyzer), `ResultScorer` (scoring), `ShowMatcher` (substring match with normalization for Movie/TextSearch), and keep `EpisodeMatcher` logic in TvSearchActor's `FilterForCaller` (it's only used there).

**Why:** MatchingPipeline mixes four concerns in one static class. TextSearchActor and MovieSearchActor call `Execute()` but discard the result — they use their own `ExpandQualities` methods instead (dead code). Quality expansion is duplicated four times with slightly different implementations. Extracting into focused components eliminates duplication and dead code.

**Alternative considered:** Refactor MatchingPipeline to be the single quality expansion path while keeping it as a class. Rejected because the class name and API surface suggest it's a pipeline, but actors cherry-pick individual methods. Clean extraction is more honest.

### Decision 2: RuleSetMatchingEngine as sole TV matching path

**Choice:** Remove the no-rules fallback in TvSearchActor. When no rules exist, return empty results and trigger auto-generation. The heuristic matching path (`MatchingPipeline.Execute` with `MatchContext`) is deleted.

**Why:** Nearly every show has rules (community or auto-generated). The heuristic path produces lower-quality results without episode mapping. Auto-generation runs in <1ms (pure CPU, pattern analysis) and produces rules that are available for the next request. Sonarr/Radarr retry automatically, so one empty response is acceptable.

**Risk:** First request for a completely unknown show returns empty. Mitigation: auto-generation is synchronous (static class, not async actor), so the rules are immediately available in the index. Only the current in-flight request misses — the very next request gets rules.

### Decision 3: Auto-generator receives items instead of sampling

**Choice:** Replace `RuleSetGeneratorActor` (which spawns as a child actor and makes its own `MediathekClient.QueryAsync` call) with a static `RuleSetGenerator` class. TvSearchActor forwards its already-fetched items to RuleSetActor via a `GenerateFromItems` message. RuleSetActor calls `RuleSetGenerator.Generate()` synchronously.

**Why:** The generator's work is pure CPU (<1ms): regex matching, pattern analysis, confidence computation. No IO needed. The current actor design exists only because it made an HTTP call. Removing the HTTP call makes the actor wrapper pure overhead. Forwarding items eliminates a duplicate API call that also bypassed rate limiting.

**Wire change:** `GenerateRuleSet(int TvdbId, string ShowName)` → `GenerateFromItems(MediathekResultItem[] Items, int TvdbId, string ShowName)`.

### Decision 4: Channels on RuleSetFile

**Choice:** Add `Channels` (string[]?) to `RuleSetFile`. Community rulesets set channels explicitly. Auto-generator derives channels from the distinct `Channel` values of provided items. `RulesResponse` carries channels alongside rules and show name. TvSearchActor uses the first channel for `.FromChannel()` in the query.

**Why:** Every show runs on specific channels (Tatort → ARD family, Das Traumschiff → ZDF). Adding channel to the query improves precision and reduces noise. The data is readily available: auto-generator gets it from the items, community rulesets can declare it.

**Alternative considered:** SearchHint as a separate value object bridging RuleSet and Query. Rejected — channel is a property of the show, not a "hint". It belongs directly on RuleSetFile.

### Decision 5: Remove topicTitle composite field

**Choice:** Remove `"topicTitle"` from `RuleSetMatchingEngine.GetFieldValue()`. Update all rules to use `field: "title"` directly. Topic validation uses filters (`{ field: "topic", op: "eq", value: "Tatort" }`), not regex lookbehinds on composite strings.

**Why:** `topicTitle` was a workaround introduced because rules mixed topic and title in regex patterns (e.g., `(?<=Tatort:\s*)(\S.*)`). With the Mediathek API returning separate fields, rules should reference actual fields. The query already filters by topic (`ByTopic`), making topic regex validation redundant. Auto-generator always generates `field: "title"` going forward.

**Impact:** All community rulesets using `topicTitle` must be updated. This reverses Decision 1 of the matching-engine-mediathek-alignment change and makes its remaining tasks (5-6) obsolete.

### Decision 6: Unified ContentFilter

**Choice:** Remove `ShouldSkipAccessibilityOnly`. Single `ShouldSkip(title, topic)` method filters both accessibility variants AND content-type keywords (trailers, teasers, previews). Used by all paths including RuleSetMatchingEngine.

**Why:** The dual-mode split was introduced so rules could decide about trailers themselves. But ContentFilter runs as a pre-filter — items it removes never reach the rules. If a show legitimately includes "Trailer" in episode titles, the show's rules won't match trailers anyway (wrong duration, wrong pattern). The split added complexity without value.

### Decision 7: Duration derived from rules for query building

**Choice:** Static helper `DeriveMinDuration(IReadOnlyList<Rule> rules) → int?` scans all rules for `duration greaterThan` filters, returns `min(values) * 60` (seconds). TvSearchActor calls this to add `.WithDuration(min: derived)` to the Mediathek query.

**Why:** Every community ruleset already has duration filters (e.g., `> 35 min` for Tatort, `> 70 min` for Das Traumschiff). Pushing this into the query reduces result count from the API, improving response times and reducing noise. The logic is a pure utility function — no new abstraction needed.

## Risks / Trade-offs

**[Risk] First request for unknown show returns empty** → Sonarr/Radarr retry searches on their configured interval (typically 15-30 minutes). Auto-generated rules are available immediately after the first request completes. Users searching manually via Prowlarr would see empty results once and get results on retry.

**[Risk] Unified ContentFilter blocks trailers that rules might want** → Reviewed all 60+ community rulesets: none intentionally match trailers. Duration filters (> 35 min) already exclude trailers implicitly. If a future edge case arises, a rule can use a filter to explicitly include items with "Trailer" in the title.

**[Risk] Removing topicTitle breaks community rulesets** → All affected rulesets are updated as part of this change. The auto-generator stops producing `topicTitle` references. New rulesets won't use it.

**[Risk] Channel filtering may be too restrictive** → `FromChannel` is only applied when `channels` is set on the RuleSet. Auto-generated rulesets derive channels from actual data. If a show moves channels, the generated ruleset would be stale — but re-generation (by deleting the generated file) fixes this. Community rulesets are maintained.
