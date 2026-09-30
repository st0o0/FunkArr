## MODIFIED Requirements

### Requirement: RuleSetManager scans ruleset directories at startup
The RuleSetManager (Singleton) SHALL scan `data/community/rulesets/*.json` and `data/local/rulesets/*.json` at startup and activate a RuleSetWorker for each discovered ruleSetId. The Manager SHALL store the scan result as known state for subsequent diff operations.

#### Scenario: Startup with community rulesets only
- **WHEN** the system starts with 5 JSON files in `data/community/rulesets/`
- **THEN** 5 RuleSetWorkers are activated via `LoadRuleSet`, one per file, using the filename (without extension) as ruleSetId

#### Scenario: Startup with community and local rulesets
- **WHEN** a ruleSetId exists in both community and local directories
- **THEN** one `LoadRuleSet` is sent for that ruleSetId, containing both file paths

#### Scenario: Local-only ruleset
- **WHEN** a ruleSetId exists only in `data/local/rulesets/`
- **THEN** a `LoadRuleSet` is sent for that ruleSetId with only the local file path

### Requirement: RuleSetWorker loads and merges ruleset files
Each RuleSetWorker (Sharded by ruleSetId) SHALL handle `LoadRuleSet` messages by loading its community and/or local JSON file(s), merging them using the existing resolve logic (community base + local overrides), and producing a MatchingConfig message. The handler SHALL be idempotent — repeated `LoadRuleSet` with the same paths produces the same result.

#### Scenario: Community-only ruleset
- **WHEN** a RuleSetWorker receives `LoadRuleSet` with only a community JSON path
- **THEN** it produces a MatchingConfig from that file alone

#### Scenario: Community + local merge
- **WHEN** a RuleSetWorker receives `LoadRuleSet` with both community and local JSON paths
- **THEN** it merges them (local overrides community rules by ID, local confidence/media override community values) and produces a single MatchingConfig

#### Scenario: Local standalone ruleset
- **WHEN** the local JSON has `standalone: true`
- **THEN** the community base is ignored and only the local ruleset is used

#### Scenario: Local disables community rules
- **WHEN** the local JSON has `disable: ["rule-id-1"]`
- **THEN** the community rule with id "rule-id-1" is excluded from the merged result

#### Scenario: Re-load with updated file
- **WHEN** a RuleSetWorker receives a second `LoadRuleSet` for the same ruleSetId with a modified community file
- **THEN** it SHALL re-read, re-merge, and re-push the updated MatchingConfig and re-register with the resolver

### Requirement: RuleSetWorker handles removal
Each RuleSetWorker SHALL handle `RemoveRuleSet` messages by sending a `RemoveMatchingConfig` to MatchMagicManager and a deregistration to RuleSetResolver.

#### Scenario: Ruleset removed
- **WHEN** a RuleSetWorker receives `RemoveRuleSet("custom-show")`
- **THEN** it SHALL send `RemoveMatchingConfig("custom-show")` to MatchMagicManager and `DeregisterRuleSet("custom-show")` to RuleSetResolver

### Requirement: RuleSetWorker pushes MatchingConfig to MatchMagicManager
After loading and merging, the RuleSetWorker SHALL send the resolved MatchingConfig message to the MatchMagicManager.

#### Scenario: Config push at startup
- **WHEN** a RuleSetWorker completes loading
- **THEN** it sends a MatchingConfig message to the MatchMagicManager singleton

### Requirement: RuleSetWorker registers with RuleSetResolver
After loading, the RuleSetWorker SHALL send a registration message to the RuleSetResolver containing the ruleSetId, topic, and aliases.

#### Scenario: Registration with aliases
- **WHEN** a RuleSetWorker loads a ruleset with `topic: "Bares für Rares"` and `aliases: ["Bares für Rares - die tägliche Show"]`
- **THEN** it sends `RegisterRuleSet("bares-fuer-rares", "Bares für Rares", ["Bares für Rares - die tägliche Show"])` to the RuleSetResolver

#### Scenario: Registration without aliases
- **WHEN** a RuleSetWorker loads a ruleset with `topic: "Schloss Einstein"` and empty aliases
- **THEN** it sends `RegisterRuleSet("schloss-einstein", "Schloss Einstein", [])` to the RuleSetResolver

### Requirement: RuleSetResolver resolves topic/alias to ruleSetId
The RuleSetResolver (Singleton) SHALL maintain an in-memory index of topic/alias → ruleSetId mappings and respond to lookup queries.

#### Scenario: Resolve by exact topic
- **WHEN** a ResolveRuleSet("Bares für Rares") query is received
- **THEN** the resolver responds with RuleSetResolved("bares-fuer-rares")

#### Scenario: Resolve by alias
- **WHEN** a ResolveRuleSet("Bares für Rares - die tägliche Show") query is received
- **THEN** the resolver responds with RuleSetResolved("bares-fuer-rares")

#### Scenario: Resolve unknown topic
- **WHEN** a ResolveRuleSet("Unknown Show") query is received
- **THEN** the resolver responds with RuleSetNotFound("Unknown Show")

#### Scenario: Registration updates overwrite
- **WHEN** a RuleSetWorker re-sends RegisterRuleSet with updated aliases
- **THEN** the resolver replaces the previous registration for that ruleSetId

### Requirement: RuleSetResolver handles deregistration
The RuleSetResolver SHALL handle `DeregisterRuleSet` messages by removing all topic/alias mappings for the given ruleSetId.

#### Scenario: Deregister existing ruleset
- **WHEN** `DeregisterRuleSet("custom-show")` is received and "custom-show" was registered
- **THEN** all topic and alias mappings for "custom-show" SHALL be removed

#### Scenario: Deregister unknown ruleset
- **WHEN** `DeregisterRuleSet("unknown")` is received and "unknown" was never registered
- **THEN** the resolver SHALL handle it silently without error

### Requirement: MatchMagicManager handles config removal
The MatchMagicManager SHALL handle `RemoveMatchingConfig` messages by removing the config for the given ruleSetId from its state.

#### Scenario: Remove existing config
- **WHEN** `RemoveMatchingConfig("custom-show")` is received
- **THEN** the config for "custom-show" SHALL be removed from the state and subsequent `ScoreItems` for that ruleSetId SHALL return default scores

#### Scenario: Remove unknown config
- **WHEN** `RemoveMatchingConfig("unknown")` is received
- **THEN** the state SHALL remain unchanged
