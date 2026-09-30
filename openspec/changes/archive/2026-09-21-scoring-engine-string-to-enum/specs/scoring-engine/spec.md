## MODIFIED Requirements

### Requirement: ScoringEngine filter evaluation produces traces

The `ScoringEngine` SHALL evaluate filter groups using `FilterGroupOp` enum values (All, Any, Not) with short-circuit semantics and produce `FilterGroupTrace` records. The `EvaluateGroupTraced` method SHALL accept a `FilterGroupOp` parameter instead of a string. All filter evaluation logic from `ScoringActor` SHALL be preserved identically.

#### Scenario: All group short-circuits on failure

- **WHEN** an All group's second condition fails
- **THEN** the third condition SHALL be marked Skipped in the trace

#### Scenario: Any group short-circuits on success

- **WHEN** an Any group's first condition passes
- **THEN** remaining conditions SHALL be marked Skipped in the trace

#### Scenario: EvaluateGroupTraced uses FilterGroupOp parameter
- **WHEN** the ScoringEngine evaluates a filter group
- **THEN** the `EvaluateGroupTraced` method SHALL accept a `FilterGroupOp` parameter instead of `string`
- **AND** the switch expression SHALL match on `FilterGroupOp.All`, `FilterGroupOp.Any`, `FilterGroupOp.Not` instead of string literals

### Requirement: ScoringEngine identification strategies produce traces

The `ScoringEngine` SHALL support all five identification strategies: SeasonAndEpisodeNumber, AbsoluteEpisodeNumber, TitleExact, TitleIncludes, AirdateExtraction. Each SHALL produce an `IdentificationTrace` with the `IdentificationStrategy` enum value directly (not `.ToString()`), attempted flag, and optional `IdentificationFailureReason` enum detail.

#### Scenario: All strategies preserved

- **WHEN** the ScoringEngine identification method is called
- **THEN** it SHALL handle all five `IdentificationStrategy` enum values identically to the current `ScoringActor` implementation

#### Scenario: Strategy passed as enum
- **WHEN** the ScoringEngine creates an IdentificationTrace
- **THEN** it SHALL pass `spec.Strategy` directly instead of `spec.Strategy.ToString()`

#### Scenario: Failure reasons as enums
- **WHEN** the ScoringEngine creates an IdentificationTrace for a failed identification
- **THEN** it SHALL use the corresponding `IdentificationFailureReason` enum value instead of a string literal
