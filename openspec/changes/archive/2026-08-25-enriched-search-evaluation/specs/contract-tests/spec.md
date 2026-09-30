## MODIFIED Requirements

### Requirement: Journal round-trip contract tests
The system SHALL have Verify-based snapshot tests that validate persistence journal wire format stability for all actor domains including the RecentMatchActor's `SearchEvaluationAdded` event type.

#### Scenario: Round-trip consistency
- **WHEN** a domain event is converted via `ToJournal()`, serialized to JSON with Newtonsoft.Json, deserialized back, and converted via `ToDomain()`
- **THEN** the resulting domain event SHALL be equal to the original

#### Scenario: Wire format snapshot
- **WHEN** a journal type is serialized to JSON
- **THEN** the JSON output SHALL match the approved `.verified.txt` snapshot

#### Scenario: SearchEvaluationAdded wire format
- **WHEN** a `SearchEvaluationAdded` event containing ItemEvaluation entries with RuleEvaluation pipeline data is serialized
- **THEN** the JSON SHALL contain int-backed enum values for EvaluationOutcome, RuleOutcome, MatchingStrategy, and FilterOp

#### Scenario: Unknown field tolerance
- **WHEN** a journal JSON string contains extra fields not defined in the current type
- **THEN** deserialization SHALL succeed without exception and the unknown fields SHALL be ignored

#### Scenario: All journal types covered
- **WHEN** listing all journal types across the persistence files
- **THEN** each type SHALL have a round-trip test and a wire format snapshot test
