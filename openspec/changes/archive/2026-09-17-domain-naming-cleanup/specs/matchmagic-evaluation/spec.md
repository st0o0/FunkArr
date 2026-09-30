## RENAMED Requirements

### Requirement: Evaluation project namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the evaluation domain namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`

### Requirement: Manager type name
FROM: `MatchMagicManager`
TO: `ScoringManager`

#### Scenario: Manager reference
- **WHEN** referencing the scoring evaluation manager
- **THEN** the type SHALL be named `ScoringManager`

### Requirement: Manager state type name
FROM: `MatchMagicManagerState`
TO: `ScoringManagerState`

#### Scenario: State reference
- **WHEN** referencing the scoring manager state
- **THEN** the type SHALL be named `ScoringManagerState`
