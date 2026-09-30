## RENAMED Requirements

### Requirement: History worker type
FROM: `MatchHistoryWorker`
TO: `ScoringHistoryWorker`

#### Scenario: Worker type name
- **WHEN** referencing the scoring history persistent actor
- **THEN** the type SHALL be named `ScoringHistoryWorker`

### Requirement: History state type
FROM: `MatchHistoryState`
TO: `ScoringHistoryState`

#### Scenario: State type name
- **WHEN** referencing the scoring history actor state
- **THEN** the type SHALL be named `ScoringHistoryState`

### Requirement: History actor key
FROM: `IMatchHistoryRegion`
TO: `IScoringHistoryRegion`

#### Scenario: Actor key interface
- **WHEN** referencing the scoring history shard region key
- **THEN** the interface SHALL be named `IScoringHistoryRegion`

### Requirement: History options type
FROM: `MatchHistoryOptions`
TO: `ScoringHistoryOptions`

#### Scenario: Options type name
- **WHEN** referencing the scoring history configuration options
- **THEN** the type SHALL be named `ScoringHistoryOptions`

### Requirement: Shard region name
FROM: `"match-history"`
TO: `"scoring-history"`

#### Scenario: Akka shard region registration
- **WHEN** registering the scoring history shard region in Akka Hosting
- **THEN** the region name SHALL be `"scoring-history"`

### Requirement: Persistence ID prefix
FROM: `"match-history-{id}"`
TO: `"scoring-history-{id}"`

#### Scenario: Actor persistence identity
- **WHEN** constructing the PersistenceId for scoring history actors
- **THEN** the prefix SHALL be `"scoring-history-"`

### Requirement: Config section name
FROM: `FunkArr:MatchHistory`
TO: `FunkArr:ScoringHistory`

#### Scenario: Configuration binding
- **WHEN** binding scoring history options from configuration
- **THEN** the section name SHALL be `FunkArr:ScoringHistory`

### Requirement: Persistence event namespace
FROM: `FunkArr.Persistence.Events.MatchHistory`
TO: `FunkArr.Persistence.Events.ScoringHistory`

#### Scenario: Event namespace
- **WHEN** referencing persistence events for scoring history
- **THEN** the namespace SHALL be `FunkArr.Persistence.Events.ScoringHistory`
