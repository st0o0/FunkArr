# matchmagic-data-model

## Purpose

Defines the core data model records and enums for the MatchMagic domain: rulesets, rules, filters, media items, match results, and quality variants.

## Requirements

### Requirement: RuleSet record
The system SHALL represent a ruleset as a sealed record with fields: Topic (string), Aliases (IReadOnlyList\<string\>?, default null), Media (MediaRef?), Confidence (float?, default null), Rules (IReadOnlyList\<Rule\>, default empty), Standalone (bool, default false), Disable (IReadOnlyList\<string\>?, default null).

#### Scenario: Deserialize community ruleset
- **WHEN** a JSON string contains `{"topic":"Tatort","aliases":["Tatort - Munster"],"media":{"tvdbId":83214,"name":"Tatort","type":"show"},"confidence":0.9,"rules":[...]}`
- **THEN** the system SHALL produce a RuleSet with Topic "Tatort", one alias, confidence 0.9, Standalone false, Disable null, and the deserialized rules

#### Scenario: Deserialize local override ruleset
- **WHEN** a JSON string contains `{"topic":"Tatort","disable":["title-fallback"],"rules":[...]}`
- **THEN** the system SHALL produce a RuleSet with Standalone false, Disable containing "title-fallback", Media null, Confidence null

#### Scenario: Deserialize standalone local ruleset
- **WHEN** a JSON string contains `{"topic":"Tatort","standalone":true,"media":{...},"confidence":0.9,"rules":[...]}`
- **THEN** the system SHALL produce a RuleSet with Standalone true

#### Scenario: Missing optional fields use defaults
- **WHEN** a JSON string omits aliases, media, confidence, standalone, and disable
- **THEN** Aliases SHALL be null, Media SHALL be null, Confidence SHALL be null, Standalone SHALL be false, Disable SHALL be null

### Requirement: MediaRef record
The system SHALL represent a media reference as a sealed record with fields: TvdbId (int?), ImdbId (string?), TmdbId (int?), Name (string), Type (MediaType enum, default Show).

#### Scenario: All IDs present
- **WHEN** a media reference has tvdbId 83214, imdbId "tt0806910", tmdbId 2116
- **THEN** the MediaRef SHALL preserve all three IDs

#### Scenario: Only name and type
- **WHEN** a media reference has only name "Tatort" and type "show"
- **THEN** TvdbId, ImdbId, TmdbId SHALL all be null and Type SHALL be MediaType.Show

#### Scenario: Movie type
- **WHEN** a media reference has type "movie"
- **THEN** Type SHALL be MediaType.Movie

#### Scenario: Unknown type fails deserialization
- **WHEN** a media reference has type "podcast"
- **THEN** deserialization SHALL fail with a JsonException

### Requirement: Rule record
The system SHALL represent a rule as a sealed record with fields: Id (string, required), Priority (int, 0 = highest), Confidence (float?, null means use file-level default), Strategy (MatchStrategy enum), Filters (FilterGroup), SeasonRegex (string?), EpisodeRegex (string?), CaptureGroup (int?, null means last group), and TitleRules (IReadOnlyList\<TitleRule\>?, default null).

#### Scenario: Rule with all fields including id
- **WHEN** a JSON rule has `{"id":"season-episode","priority":0,"confidence":0.95,"strategy":"seasonAndEpisodeNumber","seasonRegex":"...","episodeRegex":"...","filters":{...}}`
- **THEN** the Rule SHALL have Id "season-episode" and all other fields populated

#### Scenario: Rule with minimal fields
- **WHEN** a JSON rule has only id, strategy, and filters
- **THEN** Priority SHALL be 0, Confidence SHALL be null, SeasonRegex and EpisodeRegex SHALL be null, TitleRules SHALL be null

#### Scenario: Rule id validation
- **WHEN** a rule id matches pattern `^[a-z][a-z0-9-]{2,}$`
- **THEN** the id SHALL be accepted

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
The system SHALL define a FilterOp enum with values: GreaterThan, LessThan, Eq, Contains, NotContains, Regex. The value `exactMatch` SHALL NOT be accepted.

#### Scenario: Operator deserialization
- **WHEN** JSON contains op "greaterThan"
- **THEN** it SHALL deserialize to FilterOp.GreaterThan

#### Scenario: exactMatch is rejected
- **WHEN** JSON contains op "exactMatch"
- **THEN** deserialization SHALL fail with a clear error

#### Scenario: eq deserialization
- **WHEN** JSON contains op "eq"
- **THEN** it SHALL deserialize to FilterOp.Eq

### Requirement: MediaType enum
The system SHALL define a MediaType enum with values: Show, Movie. JSON serialization SHALL use camelCase strings: "show", "movie".

#### Scenario: Show deserialization
- **WHEN** JSON contains type "show"
- **THEN** it SHALL deserialize to MediaType.Show

#### Scenario: Movie deserialization
- **WHEN** JSON contains type "movie"
- **THEN** it SHALL deserialize to MediaType.Movie

#### Scenario: Unknown type rejected
- **WHEN** JSON contains type "documentary"
- **THEN** deserialization SHALL fail with a clear error

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
