## Requirements

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
After loading, the RuleSetWorker SHALL send a registration message to the RuleSetResolver containing the ruleSetId, topic, aliases, and media IDs (tvdbId, imdbId, tmdbId) extracted from the ruleset JSON.

#### Scenario: Registration with aliases and media IDs
- **WHEN** a RuleSetWorker loads a ruleset with `topic: "Tatort"`, `aliases: ["Tatort - Munster"]`, and `media: { tvdbId: 83214, imdbId: "tt0806910", tmdbId: 2116 }`
- **THEN** it sends `RegisterRuleSet("tatort", "Tatort", ["Tatort - Munster"], TvdbId: 83214, ImdbId: "tt0806910", TmdbId: 2116)` to the RuleSetResolver

#### Scenario: Registration without aliases
- **WHEN** a RuleSetWorker loads a ruleset with `topic: "Schloss Einstein"` and empty aliases and no media block
- **THEN** it sends `RegisterRuleSet("schloss-einstein", "Schloss Einstein", [], TvdbId: null, ImdbId: null, TmdbId: null)` to the RuleSetResolver

#### Scenario: Registration with partial media IDs
- **WHEN** a RuleSetWorker loads a ruleset with `media: { tvdbId: 83214 }` (no imdbId or tmdbId)
- **THEN** it sends `RegisterRuleSet` with `TvdbId: 83214, ImdbId: null, TmdbId: null`

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

### Requirement: RuleSetMerger extracts media IDs from ruleset JSON

The `RuleSetMerger.ExtractIdentity` method SHALL return media IDs (tvdbId, imdbId, tmdbId) alongside topic and aliases. The `RawRuleSet` SHALL include a `Media` property for deserializing the JSON `media` block.

#### Scenario: Community ruleset with media block

- **WHEN** a community JSON has `"media": { "tvdbId": 83214, "imdbId": "tt0806910", "tmdbId": 2116 }`
- **THEN** `ExtractIdentity` SHALL return TvdbId=83214, ImdbId="tt0806910", TmdbId=2116

#### Scenario: Local ruleset overrides media IDs

- **WHEN** community JSON has `"media": { "tvdbId": 83214 }` and local JSON has `"media": { "tvdbId": 99999 }`
- **THEN** `ExtractIdentity` SHALL return TvdbId=99999 (local overrides community)

#### Scenario: No media block

- **WHEN** a ruleset JSON has no `media` property
- **THEN** `ExtractIdentity` SHALL return null for all media IDs

#### Scenario: Standalone local with media

- **WHEN** a local JSON has `standalone: true` and `"media": { "imdbId": "tt1234567" }`
- **THEN** `ExtractIdentity` SHALL return ImdbId="tt1234567" from the local-only ruleset
