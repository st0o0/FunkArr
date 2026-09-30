## ADDED Requirements

### Requirement: RuleSet file format
The system SHALL store rulesets as JSON files with one file per show, containing a topic, media reference, source indicator, confidence score, and an ordered array of rules.

#### Scenario: Valid ruleset file structure
- **WHEN** a ruleset JSON file is read from disk
- **THEN** it SHALL deserialize into a RuleSetFile record with fields: topic (string), media (MediaReference), source (string enum: "community", "generated", "local"), confidence (float 0.0-1.0), and rules (array of Rule)

#### Scenario: Empty rules array
- **WHEN** a ruleset file has an empty rules array
- **THEN** the system SHALL treat the show as having no matching rules (fallback to generic pipeline)

### Requirement: Media reference
Each ruleset file SHALL contain a media reference with optional tvdbId (int), optional imdbId (string), name (string), and type (string, currently always "show").

#### Scenario: Lookup by TVDB ID
- **WHEN** the registry looks up a ruleset by tvdbId 329324
- **THEN** it SHALL find the ruleset file whose media.tvdbId equals 329324

#### Scenario: Missing TVDB ID
- **WHEN** a ruleset file has no tvdbId (null)
- **THEN** the system SHALL still be able to look up the ruleset by topic name

### Requirement: Rule structure
Each rule SHALL contain: priority (int, 0 = highest), filters (array of Filter), strategy (MatchingStrategy enum), optional seasonRegex (string), optional episodeRegex (string), and titleRules (array of TitleRule).

#### Scenario: Multiple rules with priority ordering
- **WHEN** a ruleset has rules with priorities [10, 0, 5]
- **THEN** the matching engine SHALL evaluate them in order [0, 5, 10] and stop at the first match

### Requirement: Filter model
Each filter SHALL have a field (string: "duration", "title", "description", "topic"), an op (enum: "greaterThan", "lessThan", "exactMatch", "contains", "regex"), and a value (string).

#### Scenario: Duration greater-than filter
- **WHEN** a filter specifies field="duration", op="greaterThan", value="35"
- **THEN** it SHALL match items whose duration in minutes exceeds 35

#### Scenario: Regex filter on title
- **WHEN** a filter specifies field="title", op="regex", value="^(?!.*Staffel).*"
- **THEN** it SHALL match items whose title matches the regex pattern

### Requirement: Matching strategy enum
The system SHALL support five matching strategies: seasonAndEpisodeNumber, itemTitleExact, itemTitleIncludes, itemTitleEqualsAirdate, byAbsoluteEpisodeNumber.

#### Scenario: Strategy deserialization
- **WHEN** a rule has strategy "seasonAndEpisodeNumber" in JSON
- **THEN** it SHALL deserialize to the MatchingStrategy.SeasonAndEpisodeNumber enum value

#### Scenario: Unknown strategy
- **WHEN** a rule has an unrecognized strategy string
- **THEN** the system SHALL log a warning and skip that rule

### Requirement: Title rule model
Each title rule SHALL have a type ("regex" or "static"), optional field (string), optional pattern (string for regex capture), and optional value (string for static text).

#### Scenario: Regex title rule extracts capture group
- **WHEN** a title rule has type="regex", field="title", pattern="^heute-show vom (\\d{1,2}\\. \\w+ \\d{4})"
- **THEN** applying it to "heute-show vom 5. Juni 2026 - heute-show (S2026/E17)" SHALL produce "5. Juni 2026"

#### Scenario: Static title rule appends text
- **WHEN** a title rule has type="static", value=" & "
- **THEN** applying it SHALL append the literal string " & " to the constructed title

#### Scenario: Combined title rules
- **WHEN** a ruleset has three title rules [regex extracting "Alice", static " & ", regex extracting "Bob"]
- **THEN** the constructed title SHALL be "Alice & Bob"

#### Scenario: Regex title rule match failure
- **WHEN** a regex title rule pattern does not match the input
- **THEN** the entire title construction SHALL fail (return null), and the rule SHALL not produce a match

### Requirement: Slug generation
The system SHALL generate filesystem-safe slugs from topic names by converting to lowercase, replacing umlauts (ä→ae, ö→oe, ü→ue, ß→ss), replacing non-alphanumeric characters with hyphens, and collapsing multiple hyphens.

#### Scenario: Topic with special characters
- **WHEN** the topic is "Feuer & Flamme"
- **THEN** the slug SHALL be "feuer-und-flamme"

#### Scenario: Topic with umlauts
- **WHEN** the topic is "Löwenzähn"
- **THEN** the slug SHALL be "loewenzaehn"

### Requirement: Community format transformation
The system SHALL parse the upstream community JSON format (flat array with JSON-in-JSON string fields for filters and titleRegexRules) and transform it into the clean per-show JSON format.

#### Scenario: Transform filters from JSON string
- **WHEN** the upstream format has filters as "[{\"attribute\":\"duration\",\"type\":\"GreaterThan\",\"value\":\"35\"}]"
- **THEN** the system SHALL parse this into a typed Filter array with field="duration", op="greaterThan", value="35"

#### Scenario: Transform titleRegexRules from JSON string
- **WHEN** the upstream format has titleRegexRules as a JSON string containing regex and static rules
- **THEN** the system SHALL parse this into a typed TitleRule array

#### Scenario: Group multiple upstream entries by topic
- **WHEN** the upstream JSON contains 3 entries with topic "Tatort" at priorities 0, 10, 20
- **THEN** the system SHALL produce one RuleSetFile with topic "Tatort" containing 3 rules sorted by priority
