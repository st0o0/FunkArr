## Requirements

### Requirement: RuleSetManager scans ruleset directories at startup
The RuleSetManager (Singleton) SHALL scan `data/community/rulesets/*.json` and `data/local/rulesets/*.json` at startup and activate a RuleSetWorker for each discovered ruleSetId.

#### Scenario: Startup with community rulesets only
- **WHEN** the system starts with 5 JSON files in `data/community/rulesets/`
- **THEN** 5 RuleSetWorkers are activated, one per file, using the filename (without extension) as ruleSetId

#### Scenario: Startup with community and local rulesets
- **WHEN** a ruleSetId exists in both community and local directories
- **THEN** one RuleSetWorker is activated for that ruleSetId, receiving both file paths

#### Scenario: Local-only ruleset
- **WHEN** a ruleSetId exists only in `data/local/rulesets/`
- **THEN** a RuleSetWorker is activated for that ruleSetId with only the local file

### Requirement: RuleSetWorker loads and merges ruleset files
Each RuleSetWorker (Sharded by ruleSetId) SHALL load its community and/or local JSON file(s), merge them using the existing resolve logic (community base + local overrides), and produce a MatchingConfig message.

#### Scenario: Community-only ruleset
- **WHEN** a RuleSetWorker has only a community JSON file
- **THEN** it produces a MatchingConfig from that file alone

#### Scenario: Community + local merge
- **WHEN** a RuleSetWorker has both community and local JSON files
- **THEN** it merges them (local overrides community rules by ID, local confidence/media override community values) and produces a single MatchingConfig

#### Scenario: Local standalone ruleset
- **WHEN** the local JSON has `standalone: true`
- **THEN** the community base is ignored and only the local ruleset is used

#### Scenario: Local disables community rules
- **WHEN** the local JSON has `disable: ["rule-id-1"]`
- **THEN** the community rule with id "rule-id-1" is excluded from the merged result

### Requirement: RuleSetWorker transforms JSON strings to enums
The RuleSetWorker SHALL transform string-based fields from the JSON (strategy, field, op, titleRule type) into the corresponding enum values defined in FunkArr.Messages before constructing the MatchingConfig.

#### Scenario: Strategy string to enum
- **WHEN** a rule JSON has `"strategy": "seasonAndEpisodeNumber"`
- **THEN** the MatchingRule receives `IdentificationStrategy.RegexCapture`

#### Scenario: Strategy mapping for title strategies
- **WHEN** a rule JSON has `"strategy": "itemTitleExact"`
- **THEN** the MatchingRule receives `IdentificationStrategy.TitleConstruction` with `TitleMatchMode.Exact`

#### Scenario: Strategy mapping for title includes
- **WHEN** a rule JSON has `"strategy": "itemTitleIncludes"`
- **THEN** the MatchingRule receives `IdentificationStrategy.TitleConstruction` with `TitleMatchMode.Contains`

#### Scenario: Strategy mapping for airdate
- **WHEN** a rule JSON has `"strategy": "itemTitleEqualsAirdate"`
- **THEN** the MatchingRule receives `IdentificationStrategy.AirdateExtraction`

#### Scenario: Strategy mapping for absolute episode
- **WHEN** a rule JSON has `"strategy": "byAbsoluteEpisodeNumber"`
- **THEN** the MatchingRule receives `IdentificationStrategy.RegexCapture` with SeasonPattern=null

#### Scenario: Invalid string value
- **WHEN** a rule JSON has an unrecognized strategy, field, or op string
- **THEN** the RuleSetWorker logs a warning and skips that rule

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
