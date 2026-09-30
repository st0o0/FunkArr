## MODIFIED Requirements

### Requirement: Mediathek fetch stage
After parallel resolution completes, TvSearchActor SHALL build the Mediathek query using information from the RulesResponse. When `Channels` is provided, the query SHALL include `.FromChannel(channels[0])`. The actor SHALL derive the minimum duration from the rules' duration filters and include `.WithDuration(min: derivedSeconds)` when a duration filter exists.

#### Scenario: Query with channel and duration from RuleSet
- **WHEN** RulesResponse provides Channels = ["ARD"] and rules have duration > 35 min filter
- **THEN** the query SHALL be `ByTopic(searchTerm).FromChannel("ARD").WithDuration(min: 2100).ExcludeFuture().Limit(200).Build()`

#### Scenario: Query without channel
- **WHEN** RulesResponse provides no channels
- **THEN** the query SHALL omit `.FromChannel()` but still include duration, ExcludeFuture, and Limit

#### Scenario: Query without duration in rules
- **WHEN** rules have no duration greaterThan filters
- **THEN** the query SHALL omit `.WithDuration()` but still include ExcludeFuture and Limit

### Requirement: Single matching path
TvSearchActor SHALL use `RuleSetMatchingEngine` as the sole matching path when rules are available. The heuristic fallback via MatchingPipeline is removed.

#### Scenario: Rules available
- **WHEN** RulesResponse contains rules
- **THEN** the entity SHALL pass items through ContentFilter, then RuleSetMatchingEngine, then QualityExpander, then ResultScorer

#### Scenario: No rules available
- **WHEN** RulesResponse contains no rules and items were fetched
- **THEN** the entity SHALL return empty results and forward items to RuleSetActor for auto-generation via `GenerateFromItems`

### Requirement: Auto-generation trigger
When no rules exist for a show, TvSearchActor SHALL forward the fetched Mediathek items to RuleSetActor via `GenerateFromItems(items, tvdbId, showName)` after returning empty results to the caller.

#### Scenario: Auto-generation triggered
- **WHEN** RulesResponse has empty rules and Mediathek items were fetched
- **THEN** the entity SHALL Tell RuleSetActor with `GenerateFromItems(items, tvdbId, showName)`

#### Scenario: Subsequent request after auto-generation
- **WHEN** a second request arrives after auto-generation completed
- **THEN** RuleSetActor SHALL return the generated rules and the entity SHALL use RuleSetMatchingEngine

### Requirement: Shared quality expansion
TvSearchActor SHALL use the shared `QualityExpander` for quality variant expansion instead of inline expansion logic.

#### Scenario: Quality expansion
- **WHEN** RuleSetMatchingEngine produces matched items
- **THEN** the entity SHALL pass them through `QualityExpander` to produce SearchResults with quality variants

### Requirement: Shared scoring
TvSearchActor SHALL use the shared `ResultScorer` for result scoring.

#### Scenario: Result scoring
- **WHEN** QualityExpander produces SearchResults
- **THEN** the entity SHALL pass them through `ResultScorer` and sort by score descending
