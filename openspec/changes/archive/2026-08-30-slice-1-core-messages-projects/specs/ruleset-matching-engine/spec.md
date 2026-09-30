## ADDED Requirements

### Requirement: IMatchStrategy interface
The system SHALL define an `IMatchStrategy` interface in `FunkArr.Core` with properties `Kind` (returning `MatchingStrategy` enum) and a `Match` method that accepts a content item, rule, and matching context.

#### Scenario: Strategy interface is implementable
- **WHEN** a new matching strategy is created
- **THEN** it SHALL implement `IMatchStrategy` with a `Kind` property and `Match` method

### Requirement: Per-strategy files
Each matching strategy SHALL be implemented in its own file under `Core/Matching/Strategies/`. The seven strategies are: `SeasonAndEpisodeNumber`, `ItemTitleExact`, `ItemTitleIncludes`, `ItemTitleEqualsAirdate`, `ByAbsoluteEpisodeNumber`, `MovieTitleMatch`, `MovieOriginalTitleMatch`.

#### Scenario: Adding a new strategy requires only new files
- **WHEN** a new matching strategy is added
- **THEN** it SHALL require only a new strategy file implementing `IMatchStrategy` and a new enum value in `MatchingStrategy`

### Requirement: Engine dispatches via strategy registry
The matching engine SHALL dispatch to strategies via a registry keyed by `MatchingStrategy` enum, replacing the inline switch statements.

#### Scenario: Engine uses registered strategy
- **WHEN** a rule with `Strategy = MatchingStrategy.SeasonAndEpisodeNumber` is evaluated
- **THEN** the engine SHALL dispatch to the `SeasonAndEpisodeNumberStrategy` implementation

## MODIFIED Requirements

### Requirement: Matching engine evaluates rules against content items
The matching engine SHALL evaluate rules against content items and return match results with traces. The engine SHALL delegate strategy-specific matching to `IMatchStrategy` implementations instead of inline switch expressions. Filter evaluation SHALL be handled by a separate `FilterEvaluator` class.

#### Scenario: Engine returns same results after refactor
- **WHEN** the same rules and items are evaluated before and after the engine split
- **THEN** the match results and traces SHALL be identical

#### Scenario: Filter evaluation is separate from strategy dispatch
- **WHEN** a rule's filter group is evaluated
- **THEN** the `FilterEvaluator` class SHALL handle the evaluation independently of strategy selection
