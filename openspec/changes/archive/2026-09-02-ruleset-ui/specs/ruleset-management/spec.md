## MODIFIED Requirements

### Requirement: RuleSetResolver resolves topic/alias to ruleSetId
The RuleSetResolver (Singleton) SHALL maintain an in-memory index of topic/alias → ruleSetId mappings and an ID index of media IDs → (ruleSetId, topic). It SHALL respond to lookup queries using topic/alias first, then falling back to ID-based resolution. It SHALL also respond to `QueryRegisteredRuleSets` by returning all registered rulesets with their identity data.

#### Scenario: Resolve by exact topic
- **WHEN** a `ResolveRuleSet("Tatort")` query is received
- **THEN** the resolver responds with `RuleSetResolved("tatort", "Tatort")`

#### Scenario: Resolve by alias
- **WHEN** a `ResolveRuleSet("Tatort - Munster")` query is received
- **THEN** the resolver responds with `RuleSetResolved("tatort", "Tatort")`

#### Scenario: Resolve unknown topic
- **WHEN** a `ResolveRuleSet("Unknown Show")` query is received with no IDs
- **THEN** the resolver responds with `RuleSetNotFound("Unknown Show")`

#### Scenario: Registration updates overwrite
- **WHEN** a RuleSetWorker re-sends RegisterRuleSet with updated aliases and media IDs
- **THEN** the resolver replaces the previous registration for that ruleSetId including all ID mappings

#### Scenario: List all registered rulesets
- **WHEN** `QueryRegisteredRuleSets` is received and 3 rulesets are registered
- **THEN** the resolver responds with `RegisteredRuleSetsResult` containing 3 entries with ruleSetId, topic, aliases, and media IDs for each

#### Scenario: List when no rulesets registered
- **WHEN** `QueryRegisteredRuleSets` is received and no rulesets are registered
- **THEN** the resolver responds with `RegisteredRuleSetsResult` containing an empty entries array

### Requirement: RuleSetManager scans ruleset directories at startup
The RuleSetManager (Singleton) SHALL scan `data/community/rulesets/*.json` and `data/local/rulesets/*.json` at startup and activate a RuleSetWorker for each discovered ruleSetId. The Manager SHALL store the scan result as known state for subsequent diff operations. The Manager SHALL also respond to `QueryRuleSetDetail` queries by re-reading the JSON files for a given ruleSetId and returning the merged result with source metadata.

#### Scenario: Startup with community rulesets only
- **WHEN** the system starts with 5 JSON files in `data/community/rulesets/`
- **THEN** 5 RuleSetWorkers are activated via `LoadRuleSet`, one per file, using the filename (without extension) as ruleSetId

#### Scenario: Startup with community and local rulesets
- **WHEN** a ruleSetId exists in both community and local directories
- **THEN** one `LoadRuleSet` is sent for that ruleSetId, containing both file paths

#### Scenario: Local-only ruleset
- **WHEN** a ruleSetId exists only in `data/local/rulesets/`
- **THEN** a `LoadRuleSet` is sent for that ruleSetId with only the local file path

#### Scenario: Query detail for known ruleset
- **WHEN** `QueryRuleSetDetail("tatort")` is received and "tatort" is in KnownRuleSets
- **THEN** the Manager re-reads the community and/or local JSON files, runs `RuleSetMerger.ExtractIdentity()` and `RuleSetMerger.Build()`, and responds with a `RuleSetDetailResult` containing identity, source metadata (paths, timestamps), and the merged matching config

#### Scenario: Query detail for unknown ruleset
- **WHEN** `QueryRuleSetDetail("nonexistent")` is received and the ruleSetId is not in KnownRuleSets
- **THEN** the Manager responds with `RuleSetNotFound("nonexistent")`

#### Scenario: Query detail when file was deleted after load
- **WHEN** `QueryRuleSetDetail("tatort")` is received but the JSON file has been deleted since the last scan
- **THEN** the Manager responds with `RuleSetNotFound("tatort")` and removes the entry from KnownRuleSets
