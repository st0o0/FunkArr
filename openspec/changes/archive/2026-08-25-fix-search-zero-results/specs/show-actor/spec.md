## MODIFIED Requirements

### Requirement: Match message
The `ShowActor` SHALL handle a `Match(Season, Episode, Items[])` message by applying ruleset matching against items using regex extraction only, without requiring TVDB episode validation. TVDB episodes are used for enrichment (episode names in results) when available, but are not required for matching. When no rules exist after auto-generation attempt, the actor SHALL return all fetched items through `ContentFilter` as fallback results instead of returning an empty list. Each fallback item SHALL be wrapped as `MatchedItemInfo(item, episode: null)`.

#### Scenario: Match with existing rules without TVDB episodes
- **WHEN** `Match(season=1, episode=null, items)` arrives and rules exist but no TVDB episodes are cached
- **THEN** the actor SHALL evaluate rules using regex extraction (S/E numbers from title patterns) and respond with matched results containing the extracted S/E numbers

#### Scenario: Match with existing rules with TVDB episodes
- **WHEN** `Match(items)` arrives and both rules and TVDB episodes are available
- **THEN** the actor SHALL evaluate rules and enrich matched results with TVDB episode names when possible

#### Scenario: Match without rules triggers inline generation
- **WHEN** `Match(items)` arrives and no rules exist
- **THEN** the actor SHALL call `RuleSetGenerator.Generate(items, tvdbId, showName, episodes)` to create rules, persist a `RulesGenerated` event, apply the new rules immediately, and respond with matched results in the same request

#### Scenario: Fallback when no rules exist after auto-generation
- **WHEN** `Match(items)` arrives and no rules exist after auto-generation attempt (generation failed or produced nothing)
- **THEN** the actor SHALL apply `ContentFilter.ShouldSkip` to filter items, wrap each passing item as `MatchedItemInfo(item, episode: null)`, and respond with `MatchedResults(fallbackItems)`

#### Scenario: Match with empty items
- **WHEN** `Match(items)` arrives with an empty items array
- **THEN** the actor SHALL respond with empty `MatchedResults`

### Requirement: Match handler produces full traces
The `ShowActor.HandleMatch` method SHALL call `EvaluateRulesWithTraces` instead of `EvaluateRulesWithoutTvdb` to produce matched, filtered, and unmatched traces. After replying to the caller, it SHALL Tell `RecentMatchActor.RecordMatch` with the full MatchRecord. When the fallback path is used (no rules), the actor SHALL NOT tell the RecentMatchActor.

#### Scenario: Match produces traces and records
- **WHEN** ShowActor receives a `Match` message with items and rules exist
- **THEN** it SHALL evaluate with traces, reply with `MatchedResults`, and Tell `RecentMatchActor.RecordMatch` with the complete MatchRecord including all trace types

#### Scenario: No rules available (fallback)
- **WHEN** ShowActor receives a `Match` message but has no effective rules after auto-generation attempt
- **THEN** it SHALL return fallback results and SHALL NOT tell the RecentMatchActor
