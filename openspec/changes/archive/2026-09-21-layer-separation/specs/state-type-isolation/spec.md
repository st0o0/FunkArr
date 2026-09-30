## ADDED Requirements

### Requirement: Domain state records hold own internal types
Domain actor state records SHALL NOT store Messages types directly as fields. State records MUST use domain-internal types, connected to Messages via explicit `ToMessage()`/`FromMessage()` mapping methods.

#### Scenario: ScoringManagerState uses internal config type
- **WHEN** `ScoringManagerState` stores matching configurations
- **THEN** it uses an internal `ScoringConfig` record, not `Messages.Scoring.MatchingConfig` directly
- **AND** conversion between `MatchingConfig` and `ScoringConfig` uses explicit mapping methods

#### Scenario: StatsCollectorState uses internal stats type
- **WHEN** `StatsCollectorState` stores stats entries
- **THEN** it uses an internal type, not `Messages.History.AllStatsResult` directly
- **AND** `FromSnapshot()` / `GetSnapshot()` map between internal and Messages types

#### Scenario: RuleSetResolverState uses internal registration type
- **WHEN** `RuleSetResolverState` stores registered ruleset data
- **THEN** it uses an internal `ResolvedRuleSet` record, extracting fields from the `RegisterRuleSet` command
- **AND** the state never stores the command message itself

### Requirement: Mapping methods are explicit extension methods
All boundary-crossing type conversions SHALL be implemented as extension methods. Method naming follows the pattern `ToXxx()` / `FromXxx()` where `Xxx` identifies the target layer.

#### Scenario: Domain to Messages mapping
- **WHEN** an actor builds a response to a caller
- **THEN** state data is converted via `ToMessage()` or `ToResult()` extension methods

#### Scenario: Messages to domain mapping
- **WHEN** an actor receives a command and updates state
- **THEN** command data is extracted via explicit field access or `FromMessage()` mapping, never stored as the raw command type
