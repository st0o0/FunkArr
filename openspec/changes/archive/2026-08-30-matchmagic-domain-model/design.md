## Context

FunkArr.MatchMagic is an empty project scaffold. It will become the core matching domain — pure logic that takes raw MediathekViewWeb items and a ruleset, and produces identified episodes/movies with quality variants and confidence scores.

The ruleset JSON schema already exists as a proven format (used by MediathekArr's community rulesets). The design challenge is mapping this schema to a C# domain model that is both faithful to the JSON structure and expressive enough to evaluate itself.

No other FunkArr domain exists yet. MatchMagic is the first domain to be built, so there are no integration constraints — only the project reference rules from the solution architecture (MatchMagic references Core only).

## Goals / Non-Goals

**Goals:**
- Map the ruleset JSON schema 1:1 to sealed C# records
- Make domain types self-evaluating: `RuleSet.Evaluate(items) → results`
- Support all five matching strategies from the schema
- Support recursive FilterGroup composition (all/any/not nesting)
- Support title construction from TitleRule chains
- Produce MatchResults that carry quality variants as data, not as a separate expansion step
- Full test coverage with real-world ruleset JSON

**Non-Goals:**
- Ruleset loading from disk/network (that's RuleSet domain responsibility)
- Auto-generation of rulesets from raw items
- Community ruleset sync from GitHub
- Any actor, persistence, or HTTP concerns
- Upstream community JSON format transformation (different schema shape → RuleSet domain)
- URL pattern analysis for quality detection (future enhancement)

## Decisions

### Decision 1: Self-evaluating records over external engine classes

**Choice:** Evaluation logic lives on the domain types themselves. `FilterGroup` has an `Evaluate` method, `Rule` has a `Match` method, `RuleSet` has an `Evaluate` method. No separate "engine" or "pipeline" classes.

**Why:** The ruleset schema defines both structure AND behavior. A `FilterGroup` with `all/any/not` inherently describes how to evaluate itself. Separating the evaluation into an external class would duplicate the schema's semantics in a different place. Keeping behavior on the types makes the code follow the data — reading a `FilterGroup` immediately tells you how it evaluates.

**Alternative considered:** Static utility class `MatchEngine.Evaluate(ruleSet, items)`. Rejected because it would grow into a god class as strategies and filter types increase. The self-evaluating approach distributes complexity along the schema's natural boundaries.

### Decision 2: MediaItem defined in MatchMagic, not Search

**Choice:** `MediaItem` lives in `FunkArr.MatchMagic` as the input contract. `FunkArr.Search` will map its API response DTOs to `MediaItem` when it's built.

**Why:** MatchMagic must be independently testable without Search. If `MediaItem` lived in Search, MatchMagic would need a reference to Search (violating domain isolation) or it would live in Messages (adding coupling to a cross-cutting project for what is really a domain-internal type). The mapping responsibility belongs to the caller.

**Alternative considered:** Define `MediaItem` in `FunkArr.Core` as a shared type. Rejected because it's not truly shared — only MatchMagic evaluates against it, and Search produces it. The producing side adapts to the consuming side's contract.

### Decision 3: Quality variants as MatchResult data, not a separate step

**Choice:** `MatchResult` contains an `IReadOnlyList<QualityVariant>` built from the `MediaItem`'s URL fields (UrlVideoHd, UrlVideo, UrlVideoLow). Each `QualityVariant` has a quality tier, URL, and estimated size. This happens inside `Rule.Match` as part of producing the result.

**Why:** Quality is a property of a matched item, not a transformation step. A MediaItem either has an HD URL or it doesn't — that's data, not logic. Pulling this out into a separate "expander" component adds a pipeline stage for what is essentially `if (url != null) add variant`. The result carries all available qualities so the caller can pick.

**Size estimation:** For now, use simple multiplier-based estimation from duration × quality tier bitrate constants. URL pattern analysis is a future enhancement.

### Decision 4: First-match-wins per item across rules

**Choice:** `RuleSet.Evaluate` iterates rules sorted by priority (0 = highest). For each item, the first rule whose filters pass AND whose strategy produces an identification wins. Later rules are not evaluated for that item.

**Why:** This matches how rulesets are authored: rule 0 is the "best" match (e.g., explicit S##E## in title), rule 10 is the fallback (e.g., episode title match). Evaluating all rules and picking the best would add complexity without value — the priority ordering already encodes the author's preference.

**Trade-off:** An item can only match one rule. If rule 0's regex captures the wrong episode number, rule 1 won't get a chance to correct it. This is acceptable because rules are authored/tested per-show and the priority ordering is intentional.

### Decision 5: JSON deserialization with System.Text.Json and snake_case

**Choice:** Use `System.Text.Json` with `JsonStringEnumConverter` and `JsonNamingPolicy.CamelCase`. The ruleset JSON uses camelCase for fields (`seasonRegex`, `episodeRegex`, `titleRules`). Deserialize directly into the sealed record types.

**Why:** System.Text.Json is the .NET default, already referenced in the project. The JSON schema uses camelCase which maps naturally to C# PascalCase with `CamelCase` naming policy. No need for Newtonsoft.Json.

### Decision 6: Filter field resolution via pattern matching

**Choice:** `Filter.Evaluate` resolves the `field` string to the corresponding `MediaItem` property via a switch expression: `"duration"` → item.Duration, `"title"` → item.Title, etc. Unknown fields return null (filter fails to match).

**Why:** The set of fields is small and fixed (duration, title, description, topic, channel, timestamp). A dictionary or reflection-based approach would add complexity for 6 fields. The switch expression is exhaustive, readable, and fast.

## Risks / Trade-offs

**[Risk] Regex performance on pathological patterns** → Community rulesets may contain expensive regexes. Mitigation: use `Regex` with `RegexOptions.Compiled` and a timeout (`TimeSpan.FromMilliseconds(100)`). Log and skip rules that timeout.

**[Risk] MatchMagic types become the wrong shape for Search integration** → When Search is built, it may need different grouping or metadata on results. Mitigation: MatchResult is a sealed record — easy to evolve by adding properties. The core contract (items in, identified results out) is stable.

**[Risk] Size estimation without URL analysis is inaccurate** → Multiplier-based estimation (duration × bitrate constant) will be off for some content. Mitigation: this is adequate for Newznab result display. Sonarr/Radarr use it for display only, not for download decisions. URL pattern analysis can be added later without changing the API.
