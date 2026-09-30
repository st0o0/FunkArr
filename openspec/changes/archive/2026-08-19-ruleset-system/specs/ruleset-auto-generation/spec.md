## ADDED Requirements

### Requirement: Pattern-based strategy detection
The RuleSetGeneratorActor SHALL analyze a sample of Mediathek results to detect the dominant title pattern and select the appropriate matching strategy.

#### Scenario: Season/episode pattern dominates
- **WHEN** 10 out of 15 sampled results contain S##/E## patterns (e.g., "(S11/E08)")
- **THEN** the system SHALL select the seasonAndEpisodeNumber strategy

#### Scenario: Date pattern dominates
- **WHEN** 8 out of 15 sampled results contain date patterns (e.g., "vom 5. Juni 2026")
- **THEN** the system SHALL select the itemTitleEqualsAirdate strategy

#### Scenario: Absolute episode number pattern
- **WHEN** 5 out of 15 sampled results contain absolute episode patterns (e.g., "Episode 1606", "Folge 42")
- **THEN** the system SHALL select the byAbsoluteEpisodeNumber strategy

#### Scenario: Topic prefix with separator
- **WHEN** 6 out of 15 sampled results start with the topic name followed by ":" or " - ", and at least 30% have separators
- **THEN** the system SHALL select the itemTitleExact strategy

#### Scenario: No clear pattern
- **WHEN** no pattern reaches the threshold of 3+ occurrences in 15 samples
- **THEN** the system SHALL select the itemTitleIncludes strategy as a fallback

#### Scenario: Strategy priority when multiple patterns present
- **WHEN** both S/E patterns (4 matches) and date patterns (3 matches) exceed the threshold
- **THEN** the system SHALL prefer S/E over date (priority: S/E > date > absolute > titleExact > titleIncludes)

### Requirement: Mediathek sampling
The generator SHALL query the Mediathek API with the show name, identify the best matching topic, and select the first 15 unique results for analysis.

#### Scenario: Topic detection by exact match
- **WHEN** the Mediathek returns results with topics ["Feuer & Flamme", "Feuerwehr Doku"] and the show name is "Feuer & Flamme"
- **THEN** the system SHALL select "Feuer & Flamme" as the topic (exact match)

#### Scenario: Topic detection by contains match
- **WHEN** the show name is "Checker Tobi" and results have topic "Checker Can, Checker Tobi und Checker Julian"
- **THEN** the system SHALL select that topic (contains match)

#### Scenario: Accessibility variant filtering
- **WHEN** the sampled results include duplicates with "(Audiodeskription)", "(Gebärdensprache)", or "(klare Sprache)" suffixes
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
The generator SHALL validate the generated ruleset against the sample results and compute a confidence score.

#### Scenario: High confidence
- **WHEN** the generated ruleset successfully matches 12 out of 15 samples (80%)
- **THEN** the confidence SHALL be 0.8 or higher

#### Scenario: Medium confidence
- **WHEN** the generated ruleset matches 6 out of 15 samples (40%)
- **THEN** the confidence SHALL be approximately 0.5

#### Scenario: Low confidence with fallback
- **WHEN** the generated ruleset matches fewer than 30% of samples
- **THEN** the system SHALL set confidence to 0.3 and fall back to itemTitleIncludes strategy

### Requirement: Generated file output
The generator SHALL write the generated ruleset as a JSON file in the generated/ directory with source="generated".

#### Scenario: File written to correct location
- **WHEN** a ruleset is generated for topic "Tagesschau"
- **THEN** the system SHALL write it to `/config/rulesets/generated/tagesschau.json`

#### Scenario: Existing generated file overwritten
- **WHEN** a ruleset already exists at generated/tagesschau.json and a new generation runs
- **THEN** the system SHALL overwrite the existing file with the new ruleset

### Requirement: Generation failure handling
The generator SHALL handle failures gracefully and report back to the registry.

#### Scenario: Mediathek API unavailable
- **WHEN** the Mediathek API call fails during generation
- **THEN** the generator SHALL log a warning and report failure to the registry without writing a file

#### Scenario: No matching topic found
- **WHEN** the Mediathek returns results but none match the show name
- **THEN** the generator SHALL log a warning and report failure to the registry
