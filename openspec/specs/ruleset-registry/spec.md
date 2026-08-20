## Purpose

Akka.NET actor managing in-memory index of show rulesets across three layers (community, generated, local), with alias indexing, startup loading, periodic community refresh, topic/alias/TVDB queries, merge-mode overrides, and auto-generation triggers.

## Requirements

### Requirement: RuleSet registry actor
The system SHALL provide a RuleSetRegistryActor registered via Akka.Hosting that maintains an in-memory index of all loaded rulesets, queryable by topic, topic alias, and TVDB ID. The actor SHALL additionally handle messages for listing all rulesets, getting a single ruleset, saving local overrides, deleting local overrides, and testing rules against the Mediathek.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the RuleSetRegistryActor SHALL be registered in the ActorSystem and resolvable via IActorRegistry

### Requirement: Topic alias indexing
The registry SHALL index all topic aliases alongside the primary topic name. A query for any alias SHALL return the same ruleset as the primary topic.

#### Scenario: Alias lookup
- **WHEN** topic "Tatort" has aliases ["Tatort - Munster", "Tatort - Schimanski"]
- **AND** a query arrives for topic "Tatort - Munster"
- **THEN** the registry SHALL return the "Tatort" ruleset

#### Scenario: Alias conflict
- **WHEN** two rulesets both claim alias "Krimi Spezial"
- **THEN** the registry SHALL log a warning and the later-loaded ruleset's alias SHALL win

### Requirement: Three-layer resolution with merge support
The system SHALL resolve rulesets with priority order: local > generated > community. Local rulesets MAY specify merge mode to compose with lower-priority layers instead of replacing them.

#### Scenario: Local override replaces (default)
- **WHEN** topic "Tatort" exists in both community/ and local/, and local has no overrides section
- **THEN** the registry SHALL return the local/ version (full replacement)

#### Scenario: Local override merges -- add rules
- **WHEN** topic "Tatort" has a community ruleset with 3 rules and a local override with `overrides: { mode: "merge", add: [rule4] }`
- **THEN** the registry SHALL return 4 rules: the 3 community rules plus rule4

#### Scenario: Local override merges -- remove rules
- **WHEN** topic "Tatort" has a community ruleset with rules at index [0,1,2] and a local override with `overrides: { mode: "merge", remove: [1] }`
- **THEN** the registry SHALL return rules at index [0,2] (rule at index 1 removed)

#### Scenario: Generated fills gap
- **WHEN** topic "Tagesschau" exists only in generated/ (not in community/ or local/)
- **THEN** the registry SHALL return the generated/ version

#### Scenario: Community baseline
- **WHEN** topic "Feuer & Flamme" exists only in community/
- **THEN** the registry SHALL return the community/ version

#### Scenario: No ruleset found
- **WHEN** no ruleset exists for the requested topic or TVDB ID in any layer
- **THEN** the registry SHALL return an empty rules response

### Requirement: Startup loading
The system SHALL load all ruleset JSON files from community/, generated/, and local/ directories at startup, building the in-memory index (including alias index) before accepting queries.

#### Scenario: Files loaded at startup
- **WHEN** the application starts with 150 files in community/, 5 in generated/, and 2 in local/
- **THEN** the registry SHALL index all rulesets with their aliases and be ready to serve queries

#### Scenario: Missing directories
- **WHEN** the rulesets/ directory or any subdirectory does not exist at startup
- **THEN** the system SHALL create the missing directories and continue with an empty index for that layer

#### Scenario: Malformed JSON file
- **WHEN** a ruleset file contains invalid JSON
- **THEN** the system SHALL log a warning with the filename and skip that file without crashing

### Requirement: Community refresh
The system SHALL periodically refresh community rulesets by querying the GitHub Releases API for the configured repository, downloading the ZIP asset, extracting it atomically, and reloading the in-memory index. The refresh interval SHALL default to 60 minutes.

#### Scenario: GitHub release refresh
- **WHEN** the refresh timer fires
- **THEN** the system SHALL query the GitHub Releases API, download the ZIP if a newer version is available, extract to `community/`, and reload the index

#### Scenario: Refresh failure
- **WHEN** the GitHub API is unreachable during a refresh attempt
- **THEN** the system SHALL log a warning and retain the existing community files and index entries

### Requirement: Query by topic
The system SHALL respond to GetRulesForTopic messages with the matching ruleset's rules, sorted by priority. Queries SHALL match against both primary topic and aliases.

#### Scenario: Exact topic match
- **WHEN** a query arrives for topic "heute-show"
- **THEN** the registry SHALL return all rules from the "heute-show" ruleset, sorted by priority ascending

#### Scenario: Alias topic match
- **WHEN** a query arrives for topic "Tatort - Munster" which is an alias for "Tatort"
- **THEN** the registry SHALL return all rules from the "Tatort" ruleset

### Requirement: Auto-generation trigger
When a query arrives for a TVDB ID with no matching ruleset in any layer, the registry SHALL spawn a RuleSetGeneratorActor to create one. The current query SHALL receive an empty response. Subsequent queries SHALL use the generated ruleset once available.

#### Scenario: First search for unknown show
- **WHEN** a query arrives for tvdbId 999999 with no existing ruleset
- **THEN** the registry SHALL start a generation process and return an empty rules response

#### Scenario: Generation already in progress
- **WHEN** a query arrives for tvdbId 999999 while generation is already running for that ID
- **THEN** the registry SHALL NOT start a duplicate generation

#### Scenario: Generation completes
- **WHEN** the RuleSetGeneratorActor completes and reports a new ruleset
- **THEN** the registry SHALL add the ruleset (with aliases) to the in-memory index immediately

### Requirement: Configurable source
The community source SHALL be configurable via `FunkArr__RuleSet__Repository` (default `"st0o0/funkarr"`) and `FunkArr__RuleSet__Version` (default `"latest"`).

#### Scenario: Defaults
- **WHEN** no `RuleSetRepository` or `RuleSetVersion` is configured
- **THEN** the system SHALL query `st0o0/funkarr` for the latest community-rulesets release

#### Scenario: Pinned version
- **WHEN** `RuleSetVersion` is set to `"1.0.0"`
- **THEN** the system SHALL fetch the `community-rulesets-v1.0.0` release specifically

#### Scenario: Custom repository
- **WHEN** `RuleSetRepository` is set to `"myorg/my-rulesets"`
- **THEN** the system SHALL query that repository's releases

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
