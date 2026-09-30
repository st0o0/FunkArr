## ADDED Requirements

### Requirement: HistoryWorker actor has test coverage
The `HistoryWorker` persistent actor SHALL have tests covering command handling, state persistence, and recovery.

#### Scenario: Record scoring result
- **WHEN** a `RecordScoringResult` command is sent to the actor
- **THEN** the actor SHALL persist the event and update its state

#### Scenario: Query history returns recorded entries
- **WHEN** scoring results have been recorded and a `QueryHistory` is sent
- **THEN** the response SHALL contain the recorded entries

#### Scenario: State recovery after restart
- **WHEN** the actor is stopped and restarted with the same persistence ID
- **THEN** the recovered state SHALL match the state before stopping

### Requirement: History persistence mapping is tested
The History domain's `MappingExtensions` for Messages<->Persistence conversion SHALL have unit tests.

#### Scenario: Message to persistence round-trip
- **WHEN** a domain message is converted to a persistence DTO and back
- **THEN** all fields SHALL be preserved
