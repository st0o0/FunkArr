## Why

FunkArr needs to match raw MediathekViewWeb search results to specific episodes and movies. MediathekArr (the reference project) does this with ~400 lines of hardcoded heuristics — filtering by runtime, aired date, title patterns, and S##E## parsing in one tangled service class. This can't handle shows with unusual patterns, offers no way for users or community to contribute matching rules, and produces binary match/no-match with no confidence signal. The matching domain is FunkArr's core differentiator and must be the first domain built.

## What Changes

- Introduce the MatchMagic domain in `FunkArr.MatchMagic` — a rich domain model where the ruleset JSON schema maps directly to C# records that evaluate themselves against raw Mediathek items
- Define the complete ruleset data model: `RuleSet`, `Rule`, `FilterGroup`, `Filter`, `TitleRule`, matching strategies, media references
- Define the input type `MediaItem` (raw MediathekViewWeb result) and output type `MatchResult` (identified episode/movie with qualities and confidence)
- Implement self-evaluating domain types: `FilterGroup` evaluates its recursive all/any/not tree, `Rule` combines filter evaluation with strategy-based episode identification, `RuleSet` orchestrates rules by priority (first match wins)
- Implement all five matching strategies: season+episode regex extraction, exact title match, title-includes match, airdate-based title match, absolute episode number
- Implement title construction from `TitleRule` chains (regex capture + static concatenation)
- Quality variants are a property of `MatchResult`, derived from the `MediaItem`'s URL fields — not a separate expansion step
- Confidence comes from the matched rule, not a separate scorer
- Add tests in `FunkArr.MatchMagic.Tests` covering real-world rulesets, filter evaluation, strategy matching, edge cases

## Capabilities

### New Capabilities
- `matchmagic-data-model`: The ruleset JSON schema as C# records — RuleSet, Rule, FilterGroup, Filter, TitleRule, MediaRef, MatchStrategy, plus MediaItem input and MatchResult output types. JSON deserialization with System.Text.Json.
- `matchmagic-evaluation`: Self-evaluating domain logic — FilterGroup tree evaluation, Rule matching (filter + strategy + title construction), RuleSet.Evaluate entry point that processes items against rules by priority.

### Modified Capabilities

## Impact

- `FunkArr.MatchMagic` project: currently empty, will contain all domain types and evaluation logic
- `FunkArr.MatchMagic.Tests` project: currently empty, will contain xUnit tests
- No impact on other projects — MatchMagic has no dependencies beyond FunkArr.Core
- No API, persistence, or actor changes
- Future domains (Search, RuleSet) will depend on MatchMagic's types
