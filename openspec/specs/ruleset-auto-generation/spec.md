## Purpose

Automatic ruleset generation by analyzing Mediathek search results to detect title patterns, derive regex patterns and duration filters, and produce ruleset JSON files with FilterGroup composition and per-rule confidence scores for shows without community-maintained rules.

## Requirements

### Requirement: Generated ruleset format
The RuleSetGeneratorActor SHALL emit rulesets in the new format with FilterGroup composition and per-rule confidence scores.

#### Scenario: Generated filters use FilterGroup
- **WHEN** the generator creates a duration filter
- **THEN** it SHALL emit it as `all: [Filter(field="duration", op="greaterThan", value=22)]` instead of a flat filter array

#### Scenario: Generated accessibility filters
- **WHEN** the generator creates a ruleset
- **THEN** it SHALL include `not: [Filter(field="title", op="regex", value="(?i)audiodesk|gebärden|gebardensprache|hörfassung|klare sprache")]` instead of relying on hardcoded skip logic

#### Scenario: Per-rule confidence on generated rules
- **WHEN** the generator creates a rule with detected confidence 0.8
- **THEN** the rule SHALL have confidence=0.8 on the individual rule record, not only on the file-level

### Requirement: Pattern-based strategy detection
The RuleSetGeneratorActor SHALL analyze a sample of Mediathek results to detect the dominant title pattern and select the appropriate matching strategy.

#### Scenario: Season/episode pattern dominates
- **WHEN** 10 out of 15 sampled results contain S##/E## patterns
- **THEN** the system SHALL select the seasonAndEpisodeNumber strategy

#### Scenario: Date pattern dominates
- **WHEN** 8 out of 15 sampled results contain date patterns
- **THEN** the system SHALL select the itemTitleEqualsAirdate strategy

#### Scenario: Absolute episode number pattern
- **WHEN** 5 out of 15 sampled results contain absolute episode patterns
- **THEN** the system SHALL select the byAbsoluteEpisodeNumber strategy

#### Scenario: Topic prefix with separator
- **WHEN** 6 out of 15 sampled results start with the topic name followed by a separator, and at least 30% have separators
- **THEN** the system SHALL select the itemTitleExact strategy

#### Scenario: No clear pattern
- **WHEN** no pattern reaches the threshold
- **THEN** the system SHALL select itemTitleIncludes as fallback

### Requirement: Mediathek sampling
The generator SHALL query the Mediathek API with the show name, identify the best matching topic, and select the first 15 unique results for analysis.

#### Scenario: Topic detection by exact match
- **WHEN** the Mediathek returns results with topics ["Feuer & Flamme", "Feuerwehr Doku"] and the show name is "Feuer & Flamme"
- **THEN** the system SHALL select "Feuer & Flamme" as the topic

#### Scenario: Accessibility variant filtering
- **WHEN** the sampled results include accessibility duplicates
- **THEN** the system SHALL exclude these variants before pattern analysis

### Requirement: Regex pattern generation
The generator SHALL derive concrete regex patterns from the actual title formats found in the sample results.

#### Scenario: Parenthesized S/E format
- **WHEN** sample titles contain "(S11/E08)" format
- **THEN** the generated seasonRegex SHALL capture the season number and episodeRegex SHALL capture the episode number from this specific format

#### Scenario: Staffel/Folge format
- **WHEN** sample titles contain "Staffel 1 Folge 3" format
- **THEN** the generated seasonRegex SHALL be "Staffel\\s*(\\d+)" and episodeRegex SHALL be "Folge\\s*(\\d+)"

#### Scenario: Date extraction regex
- **WHEN** sample titles contain "vom 5. Juni 2026" format
- **THEN** the generated titleRules SHALL contain a regex rule extracting the date portion

#### Scenario: Absolute episode number regex
- **WHEN** sample titles contain "Episode 1606" or "(1606)" format
- **THEN** the generated episodeRegex SHALL capture the absolute number

### Requirement: Duration filter generation
The generator SHALL derive a duration filter from the sample results to exclude short clips and trailers.

#### Scenario: Duration filter from samples
- **WHEN** the sampled results have durations [44, 45, 44, 45, 43, 44] minutes
- **THEN** the generated filter SHALL be greaterThan with value approximately equal to median * 0.5 (i.e., ~22 minutes)

### Requirement: Confidence scoring
The generator SHALL validate the generated ruleset against the sample results and compute a confidence score, stored on each generated rule.

#### Scenario: High confidence
- **WHEN** the generated ruleset matches 80%+ of samples
- **THEN** the per-rule confidence SHALL be 0.8 or higher

#### Scenario: Low confidence with fallback
- **WHEN** the generated ruleset matches fewer than 30% of samples
- **THEN** the system SHALL set per-rule confidence to 0.3 and fall back to itemTitleIncludes

### Requirement: Generated file output
The generator SHALL write the generated ruleset as a JSON file in the generated/ directory with source="generated", using the new FilterGroup format.

#### Scenario: File written to correct location
- **WHEN** a ruleset is generated for topic "Tagesschau"
- **THEN** the system SHALL write it to the generated/ directory with the topic slug as filename

#### Scenario: Existing generated file overwritten
- **WHEN** a ruleset already exists at generated/tagesschau.json and a new generation runs
- **THEN** the system SHALL overwrite the existing file with the new ruleset

### Requirement: Generation failure handling
The generator SHALL handle failures gracefully and report back to the registry.

#### Scenario: Mediathek API unavailable
- **WHEN** the Mediathek API call fails
- **THEN** the generator SHALL log a warning and report failure without writing a file

#### Scenario: No matching topic found
- **WHEN** no Mediathek results match the show name
- **THEN** the generator SHALL log a warning and report failure
