## MODIFIED Requirements

### Requirement: RuleSet registry actor
The system SHALL provide a RuleSetRegistryActor registered via Akka.Hosting that maintains an in-memory index of all loaded rulesets, queryable by topic, topic alias, and TVDB ID.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the RuleSetRegistryActor SHALL be registered in the ActorSystem and resolvable via IActorRegistry

### Requirement: Topic alias indexing
The registry SHALL index all topic aliases alongside the primary topic name. A query for any alias SHALL return the same ruleset as the primary topic.

#### Scenario: Alias lookup
- **WHEN** topic "Tatort" has aliases ["Tatort - Münster", "Tatort - Schimanski"]
- **AND** a query arrives for topic "Tatort - Münster"
- **THEN** the registry SHALL return the "Tatort" ruleset

#### Scenario: Alias conflict
- **WHEN** two rulesets both claim alias "Krimi Spezial"
- **THEN** the registry SHALL log a warning and the later-loaded ruleset's alias SHALL win

### Requirement: Three-layer resolution with merge support
The system SHALL resolve rulesets with priority order: local > generated > community. Local rulesets MAY specify merge mode to compose with lower-priority layers instead of replacing them.

#### Scenario: Local override replaces (default)
- **WHEN** topic "Tatort" exists in both community/ and local/, and local has no overrides section
- **THEN** the registry SHALL return the local/ version (full replacement)

#### Scenario: Local override merges — add rules
- **WHEN** topic "Tatort" has a community ruleset with 3 rules and a local override with `overrides: { mode: "merge", add: [rule4] }`
- **THEN** the registry SHALL return 4 rules: the 3 community rules plus rule4

#### Scenario: Local override merges — remove rules
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
The system SHALL periodically refresh community rulesets by fetching the configured source URL, transforming the data, and rewriting the community/ directory. The refresh interval SHALL default to 60 minutes.

#### Scenario: Scheduled refresh
- **WHEN** 60 minutes have elapsed since the last community refresh
- **THEN** the system SHALL fetch the source URL, transform the data, and update the community/ directory and in-memory index (including aliases)

#### Scenario: Refresh failure
- **WHEN** the source URL is unreachable during a refresh attempt
- **THEN** the system SHALL log a warning and retain the existing community/ files and index entries

### Requirement: Query by topic
The system SHALL respond to GetRulesForTopic messages with the matching ruleset's rules, sorted by priority. Queries SHALL match against both primary topic and aliases.

#### Scenario: Exact topic match
- **WHEN** a query arrives for topic "heute-show"
- **THEN** the registry SHALL return all rules from the "heute-show" ruleset, sorted by priority ascending

#### Scenario: Alias topic match
- **WHEN** a query arrives for topic "Tatort - Münster" which is an alias for "Tatort"
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

### Requirement: Configurable source URL
The community source URL SHALL be configurable via `FunkArr__RulesetSourceUrl` with a sensible default.

#### Scenario: Custom source URL
- **WHEN** `FunkArr__RulesetSourceUrl` is set to a custom URL
- **THEN** the system SHALL fetch community rulesets from that URL

#### Scenario: Default source URL
- **WHEN** no `FunkArr__RulesetSourceUrl` is set
- **THEN** the system SHALL use the default GitHub raw URL
