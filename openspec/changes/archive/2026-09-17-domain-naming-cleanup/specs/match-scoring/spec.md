## RENAMED Requirements

### Requirement: Scoring project namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the scoring domain project namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`

### Requirement: Scoring manager actor
FROM: `MatchMagicManager`
TO: `ScoringManager`

#### Scenario: Manager type name
- **WHEN** referencing the scoring singleton manager actor
- **THEN** the type SHALL be named `ScoringManager`

### Requirement: Scoring pool actor
FROM: `MatchMagicActor`
TO: `ScoringActor`

#### Scenario: Pool actor type name
- **WHEN** referencing the scoring pool worker actor
- **THEN** the type SHALL be named `ScoringActor`
