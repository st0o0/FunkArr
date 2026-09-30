## RENAMED Requirements

### Requirement: Scoring engine project namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the scoring engine namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`
- **AND** the `ScoringEngine` type name SHALL remain unchanged
