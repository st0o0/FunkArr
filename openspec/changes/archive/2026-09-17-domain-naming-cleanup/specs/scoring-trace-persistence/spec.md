## RENAMED Requirements

### Requirement: Trace persistence project namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the scoring trace persistence namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`

### Requirement: Persistence event namespace
FROM: `FunkArr.Persistence.Events.MatchHistory`
TO: `FunkArr.Persistence.Events.ScoringHistory`

#### Scenario: Event namespace
- **WHEN** referencing persistence events for scoring traces
- **THEN** the namespace SHALL be `FunkArr.Persistence.Events.ScoringHistory`
