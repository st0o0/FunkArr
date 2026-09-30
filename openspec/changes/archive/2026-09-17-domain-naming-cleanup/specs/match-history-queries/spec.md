## RENAMED Requirements

### Requirement: Query handler worker type
FROM: `MatchHistoryWorker`
TO: `ScoringHistoryWorker`

#### Scenario: Worker type name
- **WHEN** referencing the scoring history query handler actor
- **THEN** the type SHALL be named `ScoringHistoryWorker`

### Requirement: Query handler state type
FROM: `MatchHistoryState`
TO: `ScoringHistoryState`

#### Scenario: State type name
- **WHEN** referencing the scoring history query state
- **THEN** the type SHALL be named `ScoringHistoryState`

### Requirement: Query handler project namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the scoring history query namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`
