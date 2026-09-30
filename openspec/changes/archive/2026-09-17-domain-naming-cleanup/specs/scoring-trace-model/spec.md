## RENAMED Requirements

### Requirement: Trace model project namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the scoring trace model namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`
- **AND** all trace type names (`ItemTrace`, `RuleTrace`, etc.) SHALL remain unchanged
