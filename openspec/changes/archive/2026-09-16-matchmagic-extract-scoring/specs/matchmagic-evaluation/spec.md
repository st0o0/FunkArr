## MODIFIED Requirements

### Requirement: MatchMagicActor builds FilterGroupTrace during filter evaluation

The MatchMagicActor SHALL delegate filter evaluation to `ScoringEngine` instead of containing the evaluation logic directly. The `ScoringEngine` SHALL build a FilterGroupTrace while evaluating filters. Each condition evaluation SHALL record the field, operator, expected value, actual resolved value, and pass/fail. Short-circuited conditions SHALL be recorded with Skipped=true.

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

The MatchMagicActor SHALL delegate identification to `ScoringEngine` instead of containing the identification logic directly. The `ScoringEngine` SHALL build an IdentificationTrace after each identification attempt.

#### Scenario: Successful identification trace

- **WHEN** identification succeeds
- **THEN** the IdentificationTrace SHALL have Attempted=true, the strategy name, and Detail=null

#### Scenario: Failed identification trace

- **WHEN** identification fails (regex no match, title construction fails, no date found)
- **THEN** the IdentificationTrace SHALL have Attempted=true, the strategy name, and a Detail string describing the failure reason

#### Scenario: Skipped identification trace

- **WHEN** filters failed before identification
- **THEN** the IdentificationTrace SHALL have Attempted=false

### Requirement: MatchMagicActor records scoring history via shard region

After sending `ScoreCompleted` to the caller, the MatchMagicActor SHALL send a `RecordScoringResult` to the `IMatchHistoryRegion` shard region. The actor SHALL resolve the shard region at runtime via `Context.GetActor<IMatchHistoryRegion>()`. The actor SHALL NOT receive the history ref via constructor injection or through messages.

#### Scenario: Scoring result recorded after evaluation

- **WHEN** MatchMagicActor completes scoring with at least one candidate
- **THEN** it SHALL send a `RecordScoringResult` to the `IMatchHistoryRegion` shard region containing the request id, rule set id, origin, timestamp, candidate count, matched count, and item traces

#### Scenario: No IActorRef in messages or constructor

- **WHEN** MatchMagicManager creates an `ExecuteScoring` message for the pool
- **THEN** the message SHALL NOT contain an `IActorRef` field

#### Scenario: History region resolved at runtime

- **WHEN** MatchMagicActor is constructed
- **THEN** it SHALL resolve `IMatchHistoryRegion` via `Context.GetActor<IMatchHistoryRegion>()` and cache it as a field

### Requirement: MatchMagicActor is thin plumbing only

The MatchMagicActor SHALL contain only Akka plumbing: constructor with `Receive<T>` registration, a Handle method that calls `ScoringEngine.Score(...)`, sends results to Sender, and tells history to the shard region. All scoring logic SHALL live in `ScoringEngine`.

#### Scenario: Actor has no static scoring methods

- **WHEN** `MatchMagicActor.cs` is inspected
- **THEN** it SHALL NOT contain any `static` methods for filter evaluation, identification, regex, title construction, or date parsing

#### Scenario: Actor delegates to ScoringEngine

- **WHEN** an `ExecuteScoring` message is received
- **THEN** the actor SHALL call `ScoringEngine.Score(...)` and use the returned results
