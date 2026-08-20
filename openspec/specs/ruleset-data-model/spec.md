## Purpose

Data model for show-specific rulesets: JSON file format, media references, rule structure with FilterGroup composition, matching strategies, title rules, slug generation, override configuration, and community format transformation.

## Requirements

### Requirement: RuleSet file format
The system SHALL store rulesets as JSON files with one file per show, containing a topic, optional aliases array, media reference, source indicator, default confidence score, an ordered array of rules, and optional overrides section.

#### Scenario: Valid ruleset file structure
- **WHEN** a ruleset JSON file is read from disk
- **THEN** it SHALL deserialize into a RuleSetFile record with fields: topic (string), aliases (string array, default empty), media (MediaReference), source (string enum: "community", "generated", "local"), confidence (float 0.0-1.0, default for rules that don't specify their own), rules (array of Rule), and overrides (optional OverrideConfig)

#### Scenario: Empty rules array
- **WHEN** a ruleset file has an empty rules array
- **THEN** the system SHALL treat the show as having no matching rules (fallback to generic pipeline)

#### Scenario: Topic aliases
- **WHEN** a ruleset has topic "Tatort" with aliases ["Tatort - Munster", "Tatort - Schimanski"]
- **THEN** the system SHALL recognize all three names as referring to the same ruleset

### Requirement: Media reference
Each ruleset file SHALL contain a media reference with optional tvdbId (int), optional imdbId (string), optional tmdbId (int), name (string), and type (string, default "show").

#### Scenario: Lookup by TVDB ID
- **WHEN** the registry looks up a ruleset by tvdbId 329324
- **THEN** it SHALL find the ruleset file whose media.tvdbId equals 329324

#### Scenario: Missing TVDB ID
- **WHEN** a ruleset file has no tvdbId (null)
- **THEN** the system SHALL still be able to look up the ruleset by topic name

#### Scenario: TMDB ID preserved
- **WHEN** the community format includes a tmdbId field
- **THEN** the system SHALL preserve it in the MediaReference (not drop it silently)

### Requirement: Rule structure
Each rule SHALL contain: priority (int, 0 = highest), filter group (FilterGroup with AND/OR/NOT composition), strategy (MatchingStrategy enum), optional confidence (float 0.0-1.0, overrides file-level default), optional seasonRegex (string), optional episodeRegex (string), optional captureGroup (int, default last group), and titleRules (array of TitleRule).

#### Scenario: Multiple rules with priority ordering
- **WHEN** a ruleset has rules with priorities [10, 0, 5]
- **THEN** the matching engine SHALL evaluate them in order [0, 5, 10] and stop at the first match

#### Scenario: Rule-level confidence
- **WHEN** rule #1 has confidence 0.95 and rule #2 has no confidence set, and the file-level confidence is 0.7
- **THEN** rule #1 SHALL report confidence 0.95 and rule #2 SHALL report confidence 0.7

#### Scenario: Configurable capture group
- **WHEN** a rule has seasonRegex "S(\\d+)/E(\\d+)" with captureGroup=1
- **THEN** the engine SHALL use capture group 1 (season number), not the last group

### Requirement: Filter group model
Filters SHALL be organized into a FilterGroup supporting composite logic: `all` (AND -- all must pass), `any` (OR -- at least one must pass), and `not` (negate -- none may pass). FilterGroups SHALL be composable (a FilterGroup can contain nested FilterGroups).

#### Scenario: AND filter group
- **WHEN** a rule has filters `all: [duration > 30, duration < 90]`
- **THEN** an item with duration 45min SHALL pass (45 > 30 AND 45 < 90)

#### Scenario: OR filter group
- **WHEN** a rule has filters `any: [channel eq "ARD", channel eq "Das Erste"]`
- **THEN** an item from channel "ARD" SHALL pass, and an item from channel "ZDF" SHALL fail

#### Scenario: NOT filter group
- **WHEN** a rule has filters `not: [title contains "Audiodeskription"]`
- **THEN** an item with title "Tatort (Audiodeskription)" SHALL fail, and "Tatort" SHALL pass

#### Scenario: Combined filter groups
- **WHEN** a rule has filters `all: [duration > 30], not: [title contains "AD"], any: [channel eq "ARD", channel eq "Das Erste"]`
- **THEN** an item MUST satisfy all three groups: duration > 30 AND title does not contain "AD" AND channel is ARD or Das Erste

#### Scenario: Nested filter groups
- **WHEN** a rule has filters `any: [all: [channel eq "ARD", duration > 60], all: [channel eq "ZDF", duration > 45]]`
- **THEN** an ARD item with 70min SHALL pass, a ZDF item with 50min SHALL pass, a ZDF item with 30min SHALL fail

#### Scenario: Empty filter group
- **WHEN** a rule has no filters (empty FilterGroup)
- **THEN** all items SHALL pass the filter evaluation

#### Scenario: Backward compatibility -- flat filter list
- **WHEN** a legacy ruleset has a flat `filters` array without all/any/not grouping
- **THEN** the system SHALL treat it as `all: [...]` (AND logic, same as current behavior)

### Requirement: Filter model
Each filter SHALL have a field (string: "duration", "title", "description", "topic", "channel", "timestamp"), an op (enum: "greaterThan", "lessThan", "eq", "contains", "notContains", "regex"), and a value (string or number).

#### Scenario: Channel filter
- **WHEN** a filter specifies field="channel", op="eq", value="ARD"
- **THEN** it SHALL match items whose Channel field equals "ARD" (case-insensitive)

#### Scenario: Timestamp filter
- **WHEN** a filter specifies field="timestamp", op="greaterThan", value="2026-01-01"
- **THEN** it SHALL match items whose timestamp is after 2026-01-01

#### Scenario: NotContains filter
- **WHEN** a filter specifies field="title", op="notContains", value="Trailer"
- **THEN** it SHALL match items whose title does NOT contain "Trailer"

#### Scenario: Duration greater-than filter
- **WHEN** a filter specifies field="duration", op="greaterThan", value=35
- **THEN** it SHALL match items whose duration in minutes exceeds 35

#### Scenario: Regex filter on title
- **WHEN** a filter specifies field="title", op="regex", value="^(?!.*Staffel).*"
- **THEN** it SHALL match items whose title matches the regex pattern

### Requirement: Override configuration
Local rulesets MAY include an overrides section specifying how to compose with lower-priority layers instead of replacing them entirely.

#### Scenario: Merge mode adds rules
- **WHEN** a local ruleset has `overrides: { mode: "merge", base: "community", add: [rule] }`
- **THEN** the registry SHALL combine the community ruleset's rules with the additional local rules

#### Scenario: Merge mode removes rules
- **WHEN** a local ruleset has `overrides: { mode: "merge", base: "community", remove: [0] }`
- **THEN** the registry SHALL use the community ruleset's rules minus the rule at index 0

#### Scenario: Replace mode (default)
- **WHEN** a local ruleset has no overrides section or `overrides: { mode: "replace" }`
- **THEN** the registry SHALL replace the entire lower-priority ruleset (current behavior)

### Requirement: Matching strategy enum
The system SHALL support five matching strategies: seasonAndEpisodeNumber, itemTitleExact, itemTitleIncludes, itemTitleEqualsAirdate, byAbsoluteEpisodeNumber.

#### Scenario: Strategy deserialization
- **WHEN** a rule has strategy "seasonAndEpisodeNumber" in JSON
- **THEN** it SHALL deserialize to the MatchingStrategy.SeasonAndEpisodeNumber enum value

#### Scenario: Unknown strategy
- **WHEN** a rule has an unrecognized strategy string
- **THEN** the system SHALL log a warning and skip that rule

### Requirement: Title rule model
Each title rule SHALL have a type ("regex" or "static"), optional field (string), optional pattern (string for regex capture), optional captureGroup (int, default last group), and optional value (string for static text).

#### Scenario: Regex title rule extracts capture group
- **WHEN** a title rule has type="regex", field="title", pattern="^heute-show vom (\\d{1,2}\\. \\w+ \\d{4})"
- **THEN** applying it to "heute-show vom 5. Juni 2026 - heute-show (S2026/E17)" SHALL produce "5. Juni 2026"

#### Scenario: Regex title rule with explicit capture group
- **WHEN** a title rule has type="regex", field="title", pattern="(\\w+):\\s*(.+)", captureGroup=2
- **THEN** it SHALL use capture group 2 instead of the last group

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
The system SHALL generate filesystem-safe slugs from topic names by converting to lowercase, replacing umlauts (ae->ae, oe->oe, ue->ue, ss->ss), replacing non-alphanumeric characters with hyphens, and collapsing multiple hyphens.

#### Scenario: Topic with special characters
- **WHEN** the topic is "Feuer & Flamme"
- **THEN** the slug SHALL be "feuer-und-flamme"

#### Scenario: Topic with umlauts
- **WHEN** the topic is "Loewenzaehn"
- **THEN** the slug SHALL be "loewenzaehn"

### Requirement: Community format transformation
The system SHALL parse the upstream community JSON format (flat array with JSON-in-JSON string fields for filters and titleRegexRules) and transform it into the new model with FilterGroup composition.

#### Scenario: Transform filters from JSON string
- **WHEN** the upstream format has filters as "[{\"attribute\":\"duration\",\"type\":\"GreaterThan\",\"value\":\"35\"}]"
- **THEN** the system SHALL parse this into a FilterGroup with `all: [Filter(field="duration", op="greaterThan", value="35")]`

#### Scenario: Transform titleRegexRules from JSON string
- **WHEN** the upstream format has titleRegexRules as a JSON string containing regex and static rules
- **THEN** the system SHALL parse this into a typed TitleRule array

#### Scenario: Group multiple upstream entries by topic
- **WHEN** the upstream JSON contains 3 entries with topic "Tatort" at priorities 0, 10, 20
- **THEN** the system SHALL produce one RuleSetFile with topic "Tatort" containing 3 rules sorted by priority

#### Scenario: TMDB ID preserved from community format
- **WHEN** the upstream format includes a tmdbId field in the media object
- **THEN** the system SHALL map it to MediaReference.TmdbId instead of dropping it
