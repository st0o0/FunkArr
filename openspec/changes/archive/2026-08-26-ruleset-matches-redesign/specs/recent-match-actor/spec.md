## MODIFIED Requirements

### Requirement: Unmatched items aggregation
The actor SHALL maintain unmatched items grouped by topic, capped at 50 items per topic, derived from SearchEvaluation items with Outcome=Unmatched. Each unmatched item SHALL track a seen count and first-seen timestamp, keyed by item title + topic.

#### Scenario: Get unmatched all topics
- **WHEN** `GetUnmatched(null)` is received
- **THEN** the actor SHALL reply with all unmatched groups sorted by item count descending, each item including seenCount and firstSeen

#### Scenario: Get unmatched filtered by topic
- **WHEN** `GetUnmatched("Tatort")` is received
- **THEN** the actor SHALL reply with only the unmatched group for Tatort, each item including seenCount and firstSeen

#### Scenario: Per-topic cap
- **WHEN** a topic accumulates more than 50 unmatched items
- **THEN** the oldest items SHALL be evicted to maintain the 50-item cap

#### Scenario: Recurrence tracking — new item
- **WHEN** an unmatched item with title "Tatort Highlights 2024" appears for topic "Tatort" for the first time
- **THEN** the item SHALL be stored with seenCount=1 and firstSeen=now

#### Scenario: Recurrence tracking — repeated item
- **WHEN** an unmatched item with title "Tatort Highlights 2024" appears again for topic "Tatort"
- **THEN** the existing entry's seenCount SHALL be incremented and firstSeen SHALL remain unchanged

#### Scenario: Recurrence tracking — recovery
- **WHEN** the actor recovers from journal/snapshot
- **THEN** the seen counts and first-seen timestamps SHALL be restored

## ADDED Requirements

### Requirement: Direct fetch by evaluation ID
The actor SHALL respond to `GetById(string Id)` with the specific SearchEvaluation matching that ID, or null if not found in the ring buffer.

#### Scenario: Fetch existing evaluation
- **WHEN** `GetById("abc-123")` is received and the evaluation exists in the buffer
- **THEN** the actor SHALL reply with the matching SearchEvaluation

#### Scenario: Fetch expired evaluation
- **WHEN** `GetById("old-456")` is received and the evaluation has been evicted from the buffer
- **THEN** the actor SHALL reply with null

#### Scenario: Fetch non-existent evaluation
- **WHEN** `GetById("no-exist")` is received
- **THEN** the actor SHALL reply with null
