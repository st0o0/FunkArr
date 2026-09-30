# match-history-persistence

## MODIFIED Requirements

### Requirement: RecordScoring command naming

`RecordScoringResult` SHALL be renamed to `RecordScoring`. The old name contained "Result" which is misleading for a command.

#### Scenario: Command name
- **WHEN** the scoring history recording command is examined
- **THEN** it SHALL be named `RecordScoring`, not `RecordScoringResult`
