## ADDED Requirements

### Requirement: RuleSet registry actor
The system SHALL provide a RuleSetRegistryActor registered via Akka.Hosting that maintains an in-memory index of all loaded rulesets, queryable by topic and TVDB ID.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the RuleSetRegistryActor SHALL be registered in the ActorSystem and resolvable via IActorRegistry

### Requirement: Three-layer resolution
The system SHALL resolve rulesets with priority order: local > generated > community. When a topic exists in multiple layers, the highest-priority layer's ruleset SHALL be used.

#### Scenario: Local override takes precedence
- **WHEN** topic "Tatort" exists in both community/ and local/ directories
- **THEN** the registry SHALL return the local/ version

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
The system SHALL load all ruleset JSON files from community/, generated/, and local/ directories at startup, building the in-memory index before accepting queries.

#### Scenario: Files loaded at startup
- **WHEN** the application starts with 150 files in community/, 5 in generated/, and 2 in local/
- **THEN** the registry SHALL index all 157 rulesets and be ready to serve queries

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
- **THEN** the system SHALL fetch the source URL, transform the data, and update the community/ directory and in-memory index

#### Scenario: Refresh failure
- **WHEN** the source URL is unreachable during a refresh attempt
- **THEN** the system SHALL log a warning and retain the existing community/ files and index entries

#### Scenario: Refresh updates index
- **WHEN** a community refresh adds a new show "Neue Show" that was not previously indexed
- **THEN** the in-memory index SHALL include "Neue Show" after the refresh completes

### Requirement: Query by topic
The system SHALL respond to GetRulesForTopic messages with the matching ruleset's rules, sorted by priority.

#### Scenario: Exact topic match
- **WHEN** a query arrives for topic "heute-show"
- **THEN** the registry SHALL return all rules from the "heute-show" ruleset, sorted by priority ascending

#### Scenario: Topic with TVDB ID filter
- **WHEN** a query arrives for topic "Terra X" with tvdbId 12345
- **THEN** the registry SHALL return only rules from rulesets whose media.tvdbId matches 12345

### Requirement: Auto-generation trigger
When a query arrives for a TVDB ID with no matching ruleset in any layer, the registry SHALL spawn a RuleSetGeneratorActor to create one. The current query SHALL receive an empty response (falling back to generic pipeline). Subsequent queries SHALL use the generated ruleset once available.

#### Scenario: First search for unknown show
- **WHEN** a query arrives for tvdbId 999999 with no existing ruleset
- **THEN** the registry SHALL start a generation process and return an empty rules response

#### Scenario: Generation already in progress
- **WHEN** a query arrives for tvdbId 999999 while generation is already running for that ID
- **THEN** the registry SHALL NOT start a duplicate generation, and SHALL return an empty rules response

#### Scenario: Generation completes
- **WHEN** the RuleSetGeneratorActor completes and reports a new ruleset
- **THEN** the registry SHALL add the ruleset to the in-memory index immediately

### Requirement: Configurable source URL
The community source URL SHALL be configurable via the environment variable `FunkArr__RulesetSourceUrl` with a sensible default pointing to the RundfunkArr GitHub raw rulesets.json.

#### Scenario: Custom source URL
- **WHEN** the environment variable `FunkArr__RulesetSourceUrl` is set to a custom URL
- **THEN** the system SHALL fetch community rulesets from that URL instead of the default

#### Scenario: Default source URL
- **WHEN** no `FunkArr__RulesetSourceUrl` environment variable is set
- **THEN** the system SHALL use the default RundfunkArr GitHub raw URL
