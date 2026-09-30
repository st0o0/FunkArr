## MODIFIED Requirements

### Requirement: Record match results
The actor SHALL accept `RecordSearchEvaluation(SearchEvaluation)` messages and persist each as a `SearchEvaluationAdded` event.

#### Scenario: Record a search evaluation
- **WHEN** `RecordSearchEvaluation` is received with a SearchEvaluation
- **THEN** the actor SHALL persist `SearchEvaluationAdded` and add the record to the ring buffer

#### Scenario: Ring buffer overflow
- **WHEN** the ring buffer contains 100 records and a new `RecordSearchEvaluation` arrives
- **THEN** the oldest record SHALL be evicted and the new record added

### Requirement: Recent records query
The actor SHALL respond to `GetRecent(int Limit)` with the most recent search evaluations in reverse chronological order.

#### Scenario: Get recent with default limit
- **WHEN** `GetRecent(50)` is received and 80 records exist
- **THEN** the actor SHALL reply with the 50 most recent SearchEvaluation records sorted by timestamp descending

#### Scenario: Get recent from empty buffer
- **WHEN** `GetRecent(50)` is received and no records exist
- **THEN** the actor SHALL reply with an empty list

### Requirement: Unmatched items aggregation
The actor SHALL maintain unmatched items grouped by topic, capped at 50 items per topic, derived from SearchEvaluation items with Outcome=Unmatched.

#### Scenario: Get unmatched all topics
- **WHEN** `GetUnmatched(null)` is received
- **THEN** the actor SHALL reply with all unmatched groups sorted by item count descending

#### Scenario: Get unmatched filtered by topic
- **WHEN** `GetUnmatched("Tatort")` is received
- **THEN** the actor SHALL reply with only the unmatched group for Tatort

#### Scenario: Per-topic cap
- **WHEN** a topic accumulates more than 50 unmatched items
- **THEN** the oldest items SHALL be evicted to maintain the 50-item cap
