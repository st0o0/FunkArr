## MODIFIED Requirements

### Requirement: MatchMagicManager is a singleton scoring actor

The MatchMagicManager SHALL be a Cluster Singleton actor that holds loaded RuleSets in memory and accepts scoring requests. It SHALL resolve the MatchHistory ShardRegion at startup and include it in ExecuteScoring messages to pool workers.

#### Scenario: Score items with loaded RuleSet

- **WHEN** a ScoreItems message is received and a matching RuleSet is loaded
- **THEN** the Manager SHALL send ExecuteScoring (with Config, Items, RequestId, Origin, and HistoryRef) to the Router Pool with the original Sender preserved, and the pool worker SHALL respond with ScoreCompleted containing scored and ranked results

#### Scenario: Score items with no RuleSet loaded

- **WHEN** a ScoreItems message is received but no RuleSet is loaded (or the requested RuleSetId is not found)
- **THEN** the Manager SHALL respond with ScoreCompleted(RequestId, defaults) where all items have a default score and Matched=false

## MODIFIED Requirements

### Requirement: ScoreItems requires ruleSetId

The ScoreItems message SHALL require a non-null RuleSetId, a RequestId (Guid) for correlation, a ScoringOrigin for provenance tracking, and a Candidates array.

#### Scenario: ScoreItems with all fields

- **WHEN** SearchWorker sends ScoreItems with RequestId=abc, RuleSetId="die-anstalt", Origin=ScoringOrigin("sonarr", "Die Anstalt"), Candidates=[...]
- **THEN** MatchMagicManager looks up config for "die-anstalt" and forwards RequestId and Origin to the pool worker

#### Scenario: ScoringOrigin record

- **WHEN** a ScoringOrigin is constructed
- **THEN** it SHALL contain Source (string) and Query (string)
