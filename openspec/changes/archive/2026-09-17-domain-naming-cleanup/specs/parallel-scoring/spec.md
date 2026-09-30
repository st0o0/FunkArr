## RENAMED Requirements

### Requirement: Parallel scoring project namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the parallel scoring namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`

### Requirement: Pool actor type
FROM: `MatchMagicActor`
TO: `ScoringActor`

#### Scenario: Actor type name
- **WHEN** referencing the pooled scoring actor
- **THEN** the type SHALL be named `ScoringActor`
