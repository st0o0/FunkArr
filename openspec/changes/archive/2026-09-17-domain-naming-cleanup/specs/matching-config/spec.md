## RENAMED Requirements

### Requirement: Config manager namespace
FROM: `FunkArr.MatchMagic`
TO: `FunkArr.Scoring`

#### Scenario: Namespace reference
- **WHEN** referencing the matching config management namespace
- **THEN** `FunkArr.Scoring` SHALL be used instead of `FunkArr.MatchMagic`

### Requirement: Config manager actor key
FROM: `IMatchMagicManager`
TO: `IScoringManager`

#### Scenario: Actor key interface
- **WHEN** referencing the scoring manager actor key for config dispatch
- **THEN** the interface SHALL be named `IScoringManager`

### Requirement: Config manager type
FROM: `MatchMagicManager`
TO: `ScoringManager`

#### Scenario: Manager type name
- **WHEN** referencing the manager that holds matching configs
- **THEN** the type SHALL be named `ScoringManager`
