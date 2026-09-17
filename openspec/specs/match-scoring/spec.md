## ADDED Requirements

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

### Requirement: MatchMagicManager manages RuleSet state

The Manager SHALL maintain a `Dictionary<string, RuleSet>` of loaded RuleSets and support loading/unloading.

#### Scenario: Load a RuleSet

- **WHEN** a LoadRuleSet message with an id and JSON content is received
- **THEN** the Manager SHALL deserialize the RuleSet and store it in state

#### Scenario: Unload a RuleSet

- **WHEN** an UnloadRuleSet message with an id is received
- **THEN** the Manager SHALL remove the RuleSet from state

### Requirement: MatchMagicManager wraps pure logic without Akka dependency in MatchMagic library

The existing MatchMagic library (RuleSet, Rule, Filter, FilterGroup, etc.) SHALL remain pure — no Akka references. The Manager actor is the only bridge between the actor system and the pure evaluation logic.

#### Scenario: Pure library independence

- **WHEN** the FunkArr.MatchMagic project is compiled
- **THEN** it SHALL have no dependency on Akka.NET packages
