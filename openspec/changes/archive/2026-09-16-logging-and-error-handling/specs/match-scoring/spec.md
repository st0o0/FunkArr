## MODIFIED Requirements

### Requirement: MatchMagicManager is a singleton scoring actor

The MatchMagicManager SHALL be a Cluster Singleton actor that holds loaded RuleSets in memory and accepts scoring requests. It SHALL resolve the MatchHistory ShardRegion at startup and include it in ExecuteScoring messages to pool workers.

The MatchMagicActor QueryDetail handler SHALL return a typed response instead of `object`. It SHALL use a discriminated response type that covers the possible query detail result shapes.

#### Scenario: Score items with loaded RuleSet

- **WHEN** a ScoreItems message is received and a matching RuleSet is loaded
- **THEN** the Manager SHALL send ExecuteScoring (with Config, Items, RequestId, Origin, and HistoryRef) to the Router Pool with the original Sender preserved, and the pool worker SHALL respond with ScoreCompleted containing scored and ranked results

#### Scenario: Score items with no RuleSet loaded

- **WHEN** a ScoreItems message is received but no RuleSet is loaded (or the requested RuleSetId is not found)
- **THEN** the Manager SHALL respond with ScoreCompleted(RequestId, defaults) where all items have a default score and Matched=false

#### Scenario: QueryDetail returns typed response

- **WHEN** a QueryDetail message is handled by the MatchMagicActor
- **THEN** it SHALL respond with a typed record (not `object`) that the caller can pattern-match on
