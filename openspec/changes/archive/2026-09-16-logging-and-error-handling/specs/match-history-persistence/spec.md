## MODIFIED Requirements

### Requirement: MatchHistoryWorker is a sharded entity actor

The MatchHistoryWorker SHALL be a sharded entity actor keyed by RuleSetId. It SHALL use the naming convention `*Worker` (sharded entity) and be registered in the actor system under shard region "match-history".

The MatchHistoryWorker PersistenceId SHALL be correctly initialized without dead field references. It SHALL use the format `"match-history-{ruleSetId}"` derived from `Context.Self.Path.Name`.

#### Scenario: Shard routing by RuleSetId

- **WHEN** a RecordScoringResult message with RuleSetId="tatort" is sent to the MatchHistory ShardRegion
- **THEN** it SHALL be routed to the MatchHistoryWorker instance for "tatort"

#### Scenario: Different RuleSets have independent workers

- **WHEN** RecordScoringResult messages arrive for "tatort" and "heute-show"
- **THEN** each SHALL be handled by a separate MatchHistoryWorker instance with independent state

#### Scenario: PersistenceId format

- **WHEN** a MatchHistoryWorker is created for RuleSetId "tatort"
- **THEN** its PersistenceId SHALL be "match-history-tatort"
- **AND** the PersistenceId SHALL NOT reference a dead `field` keyword or uninitialized backing field
