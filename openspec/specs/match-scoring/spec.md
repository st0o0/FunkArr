## ADDED Requirements

### Requirement: MatchMagicManager is a singleton scoring actor

The MatchMagicManager SHALL be a Cluster Singleton actor that holds loaded RuleSets in memory and accepts scoring requests.

#### Scenario: Score items with loaded RuleSet

- **WHEN** a ScoreItems message is received and a matching RuleSet is loaded
- **THEN** the Manager SHALL evaluate items against the RuleSet using the existing pure MatchMagic logic and respond with ScoreCompleted containing scored and ranked results

#### Scenario: Score items with no RuleSet loaded

- **WHEN** a ScoreItems message is received but no RuleSet is loaded (or the requested RuleSetId is not found)
- **THEN** the Manager SHALL respond with ScoreCompleted where all items have a default score and Matched=false

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
