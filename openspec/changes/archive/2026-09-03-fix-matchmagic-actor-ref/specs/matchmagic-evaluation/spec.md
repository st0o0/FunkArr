## ADDED Requirements

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
