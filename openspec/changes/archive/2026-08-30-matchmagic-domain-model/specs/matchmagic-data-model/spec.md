## ADDED Requirements

### Requirement: RuleSet record
The system SHALL represent a ruleset as a sealed record with fields: Topic (string), Aliases (IReadOnlyList\<string\>, default empty), Media (MediaRef), Source (string: "community", "generated", "local"), Confidence (float 0.0-1.0, default for rules without their own), and Rules (IReadOnlyList\<Rule\>).

#### Scenario: Deserialize complete ruleset
- **WHEN** a JSON string contains `{"topic":"Tatort","aliases":["Tatort - Münster"],"media":{"tvdbId":83214,"name":"Tatort","type":"show"},"source":"community","confidence":0.9,"rules":[...]}`
- **THEN** the system SHALL produce a RuleSet with Topic "Tatort", one alias, source "community", confidence 0.9, and the deserialized rules

#### Scenario: Missing optional fields use defaults
- **WHEN** a JSON string omits the aliases field
- **THEN** the RuleSet SHALL have an empty Aliases list

### Requirement: MediaRef record
The system SHALL represent a media reference as a sealed record with fields: TvdbId (int?), ImdbId (string?), TmdbId (int?), Name (string), Type (string, default "show").

#### Scenario: All IDs present
- **WHEN** a media reference has tvdbId 83214, imdbId "tt0806910", tmdbId 2116
- **THEN** the MediaRef SHALL preserve all three IDs

#### Scenario: Only name and type
- **WHEN** a media reference has only name "Tatort" and type "show"
- **THEN** TvdbId, ImdbId, TmdbId SHALL all be null

### Requirement: Rule record
The system SHALL represent a rule as a sealed record with fields: Priority (int, 0 = highest), Confidence (float?, null means use file-level default), Strategy (MatchStrategy enum), Filters (FilterGroup), SeasonRegex (string?), EpisodeRegex (string?), CaptureGroup (int?, null means last group), and TitleRules (IReadOnlyList\<TitleRule\>, default empty).

#### Scenario: Rule with all fields
- **WHEN** a JSON rule has priority 0, confidence 0.95, strategy "seasonAndEpisodeNumber", seasonRegex, episodeRegex, and filters
- **THEN** the Rule SHALL deserialize with all fields populated

#### Scenario: Rule with minimal fields
- **WHEN** a JSON rule has only priority, strategy, and filters
- **THEN** Confidence SHALL be null, SeasonRegex and EpisodeRegex SHALL be null, TitleRules SHALL be empty

### Requirement: MatchStrategy enum
The system SHALL define a MatchStrategy enum with values: SeasonAndEpisodeNumber, ItemTitleExact, ItemTitleIncludes, ItemTitleEqualsAirdate, ByAbsoluteEpisodeNumber.

#### Scenario: Strategy deserialization
- **WHEN** JSON contains strategy "seasonAndEpisodeNumber"
- **THEN** it SHALL deserialize to MatchStrategy.SeasonAndEpisodeNumber

#### Scenario: Unknown strategy
- **WHEN** JSON contains an unrecognized strategy string
- **THEN** deserialization SHALL fail with a clear error

### Requirement: FilterGroup record
The system SHALL represent a filter group as a sealed record with fields: All (IReadOnlyList\<FilterNode\>?), Any (IReadOnlyList\<FilterNode\>?), Not (IReadOnlyList\<FilterNode\>?). A FilterNode is either a Filter or a nested FilterGroup, enabling recursive composition.

#### Scenario: Simple AND group
- **WHEN** JSON has `{"all":[{"field":"duration","op":"greaterThan","value":"60"}]}`
- **THEN** the FilterGroup SHALL have one All entry containing a duration filter

#### Scenario: Nested groups
- **WHEN** JSON has `{"all":[{"field":"duration","op":"greaterThan","value":"30"},{"any":[{"field":"channel","op":"eq","value":"ARD"},{"field":"channel","op":"eq","value":"ZDF"}]}]}`
- **THEN** the FilterGroup SHALL have an All list containing a Filter and a nested FilterGroup with Any

#### Scenario: Empty filter group
- **WHEN** JSON has `{}` or all three lists are null
- **THEN** the FilterGroup SHALL be considered empty (passes all items during evaluation)

### Requirement: Filter record
The system SHALL represent a filter as a sealed record with fields: Field (string), Op (FilterOp enum), Value (string).

#### Scenario: Duration filter
- **WHEN** JSON has `{"field":"duration","op":"greaterThan","value":"60"}`
- **THEN** the Filter SHALL have Field "duration", Op GreaterThan, Value "60"

### Requirement: FilterOp enum
The system SHALL define a FilterOp enum with values: GreaterThan, LessThan, Eq, Contains, NotContains, Regex.

#### Scenario: Operator deserialization
- **WHEN** JSON contains op "greaterThan"
- **THEN** it SHALL deserialize to FilterOp.GreaterThan

### Requirement: TitleRule record
The system SHALL represent a title rule as a sealed record with fields: Type (string: "regex" or "static"), Field (string?), Pattern (string?), CaptureGroup (int?, null means last group), Value (string?).

#### Scenario: Regex title rule
- **WHEN** JSON has `{"type":"regex","field":"title","pattern":"^Tatort[^:]*:\\s*(.+)"}`
- **THEN** the TitleRule SHALL have Type "regex", Field "title", Pattern with the regex, CaptureGroup null

#### Scenario: Static title rule
- **WHEN** JSON has `{"type":"static","value":" & "}`
- **THEN** the TitleRule SHALL have Type "static", Value " & ", Field null, Pattern null

### Requirement: MediaItem record
The system SHALL represent a raw MediathekViewWeb result as a sealed record with fields: Topic (string), Title (string), Description (string?), Channel (string), Timestamp (long, Unix epoch seconds), Duration (int, seconds), UrlVideoHd (string?), UrlVideo (string?), UrlVideoLow (string?), UrlSubtitle (string?), UrlWebsite (string?), Size (long).

#### Scenario: Complete media item
- **WHEN** a MediaItem is constructed with all fields populated
- **THEN** all fields SHALL be accessible and non-null where specified

#### Scenario: Missing optional URLs
- **WHEN** a MediaItem has no UrlVideoHd
- **THEN** UrlVideoHd SHALL be null

### Requirement: MatchResult record
The system SHALL represent a match result as a sealed record with fields: Item (MediaItem), MatchedRule (Rule), Identification (EpisodeIdentification), ConstructedTitle (string?), Confidence (float), Qualities (IReadOnlyList\<QualityVariant\>).

#### Scenario: Match result with all qualities
- **WHEN** a MediaItem has UrlVideoHd, UrlVideo, and UrlVideoLow
- **THEN** the MatchResult SHALL have three QualityVariant entries

#### Scenario: Match result confidence from rule
- **WHEN** a Rule has confidence 0.95
- **THEN** the MatchResult SHALL have Confidence 0.95

#### Scenario: Match result confidence from file default
- **WHEN** a Rule has no confidence set and the RuleSet has confidence 0.9
- **THEN** the MatchResult SHALL have Confidence 0.9

### Requirement: EpisodeIdentification record
The system SHALL represent an episode identification as a sealed record with fields: Season (string?), Episode (string?), Title (string?).

#### Scenario: Season and episode identified
- **WHEN** a regex strategy extracts season "01" and episode "05"
- **THEN** the EpisodeIdentification SHALL have Season "01", Episode "05"

#### Scenario: Title-only identification
- **WHEN** a title match strategy produces a title but no season/episode numbers
- **THEN** Season and Episode SHALL be null, Title SHALL be the extracted title

### Requirement: QualityVariant record
The system SHALL represent a quality variant as a sealed record with fields: Quality (Quality enum), Url (string), EstimatedSizeBytes (long).

#### Scenario: HD1080 variant
- **WHEN** a MediaItem has UrlVideoHd = "https://example.com/video_hd.mp4"
- **THEN** the QualityVariant SHALL have Quality HD1080 and the URL

### Requirement: Quality enum
The system SHALL define a Quality enum with values: HD1080, HD720, SD.

#### Scenario: Quality ordering
- **WHEN** comparing quality values
- **THEN** HD1080 SHALL be higher than HD720 SHALL be higher than SD
