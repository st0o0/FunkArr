## MODIFIED Requirements

### Requirement: MatchMagicActor is stateless

The MatchMagicActor SHALL hold no state. It receives ExecuteScoring messages containing config, items, RequestId, Origin, and HistoryRef. It SHALL reply to Sender with ScoreCompleted and fire-and-forget a RecordScoringResult to the HistoryRef.

#### Scenario: Stateless processing with trace

- **WHEN** MatchMagicActor receives ExecuteScoring with config, items, RequestId, Origin, and HistoryRef
- **THEN** it SHALL evaluate all items against the config's rules, build a full ItemTrace per item, send ScoreCompleted(RequestId, scored) to Sender, and send RecordScoringResult(RequestId, RuleSetId, Origin, Timestamp, CandidateCount, MatchedCount, ItemTraces) to HistoryRef

#### Scenario: History emission is fire-and-forget

- **WHEN** MatchMagicActor sends RecordScoringResult to HistoryRef
- **THEN** it SHALL use Tell (not Ask) and SHALL NOT wait for confirmation or handle failure

#### Scenario: ScoreCompleted sent before history

- **WHEN** MatchMagicActor completes evaluation
- **THEN** it SHALL send ScoreCompleted to Sender first, then send RecordScoringResult to HistoryRef

## ADDED Requirements

### Requirement: MatchMagicActor builds FilterGroupTrace during filter evaluation

The MatchMagicActor SHALL build a FilterGroupTrace while evaluating filters. Each condition evaluation SHALL record the field, operator, expected value, actual resolved value, and pass/fail. Short-circuited conditions SHALL be recorded with Skipped=true.

#### Scenario: All conditions evaluated

- **WHEN** a FilterGroup with operator All has 3 conditions and all pass
- **THEN** the FilterGroupTrace SHALL contain 3 FilterNodeTraces with Passed=true and Skipped=false, each with ActualValue populated

#### Scenario: Short-circuit in All group

- **WHEN** a FilterGroup with operator All has 3 conditions and condition 2 fails
- **THEN** the FilterGroupTrace SHALL contain condition 1 with Passed=true, condition 2 with Passed=false (with ActualValue), and condition 3 with Skipped=true and ActualValue=null

#### Scenario: Nested group traced recursively

- **WHEN** a FilterGroup contains a nested FilterGroup
- **THEN** the outer FilterGroupTrace SHALL contain a FilterNodeTrace of type Group with a nested FilterGroupTrace

### Requirement: MatchMagicActor builds IdentificationTrace during identification

The MatchMagicActor SHALL build an IdentificationTrace after each identification attempt.

#### Scenario: Successful identification trace

- **WHEN** identification succeeds
- **THEN** the IdentificationTrace SHALL have Attempted=true, the strategy name, and Detail=null

#### Scenario: Failed identification trace

- **WHEN** identification fails (regex no match, title construction fails, no date found)
- **THEN** the IdentificationTrace SHALL have Attempted=true, the strategy name, and a Detail string describing the failure reason

#### Scenario: Skipped identification trace

- **WHEN** filters failed before identification
- **THEN** the IdentificationTrace SHALL have Attempted=false
