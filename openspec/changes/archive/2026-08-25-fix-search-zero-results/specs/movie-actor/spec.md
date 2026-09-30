## MODIFIED Requirements

### Requirement: Match message
The `MovieActor` SHALL handle a `Match(Items[])` message by applying ruleset matching against movie identity and responding with `MatchedResults`. When no rules exist after auto-generation attempt, the actor SHALL return all fetched items through `ContentFilter` as fallback results instead of returning an empty list. Each fallback item SHALL be wrapped as `MatchedItemInfo(item, episode: null)`.

#### Scenario: Match with existing rules
- **WHEN** `Match(items)` arrives and rules exist
- **THEN** the actor SHALL call `RuleSetMatchingEngine.EvaluateMovieRules(items, rules, movieInfo)` and respond with matched results

#### Scenario: Match without rules triggers inline generation
- **WHEN** `Match(items)` arrives and no rules exist
- **THEN** the actor SHALL call `RuleSetGenerator.GenerateForMovie(items, movieInfo)` to create rules, persist a `RulesGenerated` event, apply the new rules immediately, and respond with matched results

#### Scenario: Fallback to original title
- **WHEN** matching with the primary title yields zero results and an original title differs from the primary title
- **THEN** the actor SHALL retry matching using the original title before responding

#### Scenario: Fallback when no rules exist after auto-generation
- **WHEN** `Match(items)` arrives and no rules exist after auto-generation attempt (generation failed or produced nothing)
- **THEN** the actor SHALL apply `ContentFilter.ShouldSkip` to filter items, wrap each passing item as `MatchedItemInfo(item, episode: null)`, and respond with `MatchedResults(fallbackItems)`

#### Scenario: Fallback with empty items
- **WHEN** `Match(items)` arrives with an empty items array and no rules exist
- **THEN** the actor SHALL respond with empty `MatchedResults`

### Requirement: Match handler produces full traces
The `MovieActor.HandleMatch` method SHALL produce matched, filtered, and unmatched traces. After replying to the caller, it SHALL Tell `RecentMatchActor.RecordMatch` with the full MatchRecord. When the fallback path is used (no rules), the actor SHALL NOT tell the RecentMatchActor.

#### Scenario: Match produces traces and records
- **WHEN** MovieActor receives a `Match` message with items and rules exist
- **THEN** it SHALL evaluate with traces, reply with `MatchedResults`, and Tell `RecentMatchActor.RecordMatch` with the complete MatchRecord including all trace types

#### Scenario: No rules available (fallback)
- **WHEN** MovieActor receives a `Match` message but has no effective rules after auto-generation attempt
- **THEN** it SHALL return fallback results and SHALL NOT tell the RecentMatchActor
