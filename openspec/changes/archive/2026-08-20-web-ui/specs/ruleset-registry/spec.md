## MODIFIED Requirements

### Requirement: RuleSet registry actor
The system SHALL provide a RuleSetRegistryActor registered via Akka.Hosting that maintains an in-memory index of all loaded rulesets, queryable by topic, topic alias, and TVDB ID. The actor SHALL additionally handle messages for listing all rulesets, getting a single ruleset, saving local overrides, deleting local overrides, and testing rules against the Mediathek.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the RuleSetRegistryActor SHALL be registered in the ActorSystem and resolvable via IActorRegistry

## ADDED Requirements

### Requirement: List all rulesets message
The RuleSetRegistryActor SHALL handle a `GetAllRulesets` message and respond with metadata for every registered topic: topic name, source, rule count, media reference, and aliases.

#### Scenario: List all topics
- **WHEN** a `GetAllRulesets` message is received
- **THEN** the actor SHALL respond with a list of all topics from the in-memory index, each with topic, source, rule count, media name, TVDB ID, and aliases

### Requirement: Get single ruleset message
The RuleSetRegistryActor SHALL handle a `GetRuleSet` message and respond with the full RuleSetFile for the requested topic.

#### Scenario: Topic exists
- **WHEN** a `GetRuleSet("tatort")` message is received and the topic exists
- **THEN** the actor SHALL respond with the full RuleSetFile including resolved rules

#### Scenario: Topic not found
- **WHEN** a `GetRuleSet("nonexistent")` message is received
- **THEN** the actor SHALL respond with a not-found response

### Requirement: Save local override message
The RuleSetRegistryActor SHALL handle a `SaveLocalRuleSet` message that writes a RuleSetFile to `data/rulesets/local/` and reloads the in-memory index.

#### Scenario: Save and reload
- **WHEN** a `SaveLocalRuleSet` message is received with a valid RuleSetFile
- **THEN** the actor SHALL write the file to the local directory using RuleSetFileWriter and reload all layers from disk

### Requirement: Delete local override message
The RuleSetRegistryActor SHALL handle a `DeleteLocalRuleSet` message that removes a local override file and reloads.

#### Scenario: Delete existing
- **WHEN** a `DeleteLocalRuleSet("tatort")` message is received and a local file exists
- **THEN** the actor SHALL delete the file and reload, falling back to community/generated

#### Scenario: Delete non-existent
- **WHEN** a `DeleteLocalRuleSet("tatort")` message is received and no local file exists
- **THEN** the actor SHALL respond with a not-found indicator

### Requirement: Test rules message
The RuleSetRegistryActor SHALL handle a `TestRules` message by searching the Mediathek for the topic, optionally fetching TVDB episodes, running `RuleSetMatchingEngine.EvaluateRulesWithTraces`, and returning the trace results.

#### Scenario: Test with TVDB
- **WHEN** a `TestRules` message is received with topic "Tatort", TVDB ID 83214, and a set of rules
- **THEN** the actor SHALL search the Mediathek, fetch TVDB episodes, evaluate rules with traces, and respond with matched/filtered/unmatched trace arrays

#### Scenario: Test without TVDB
- **WHEN** a `TestRules` message is received without a TVDB ID
- **THEN** the actor SHALL search the Mediathek, evaluate rules with an empty TVDB episode list, and respond with trace results
