## ADDED Requirements

### Requirement: Extraction terminology for scoring rules
The ruleset schema and code SHALL use `extraction` (schema) / `ExtractionMethod` (enum) / `ExtractionSpec` (record) for the concept of extracting season/episode from Mediathek item titles.

#### Scenario: Ruleset JSON uses extraction field
- **WHEN** a ruleset JSON defines a rule's method for S/E extraction
- **THEN** the field SHALL be named `extraction` (not `strategy`)

#### Scenario: Code references extraction method
- **WHEN** code references the enum for extraction methods
- **THEN** it SHALL use `ExtractionMethod` (not `IdentificationStrategy`)

### Requirement: Matching terminology for TVDB/TMDB episode matching
The MetadataMatching domain SHALL use "Matcher" for classes that match candidates against external metadata, and "Matching" for the orchestrating actor and project name.

#### Scenario: Episode matching class
- **WHEN** code references the class that fuzzy-matches candidates against TVDB episodes
- **THEN** it SHALL be named `EpisodeMatcher` (not `EpisodeResolver`)

#### Scenario: Metadata matching manager
- **WHEN** code references the actor that orchestrates TVDB/TMDB matching
- **THEN** it SHALL be named `MetadataMatchingManager` (not `MetadataResolverManager`)

### Requirement: Resolver terminology reserved for lookups
The term "Resolver" SHALL only be used for lookup operations that map an input to a known entity (e.g. RuleSetResolver maps topic/ID to ruleSetId).

#### Scenario: RuleSetResolver name unchanged
- **WHEN** code references the actor that maps topics and IDs to ruleset IDs
- **THEN** it SHALL remain named `RuleSetResolver`
