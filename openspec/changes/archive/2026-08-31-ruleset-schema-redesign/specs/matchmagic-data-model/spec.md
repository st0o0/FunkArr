# matchmagic-data-model

## MODIFIED Requirements

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

## ADDED Requirements

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

## REMOVED Requirements

### Requirement: Source field on RuleSet

**Reason**: The `Source` field (string: "community", "generated", "local") is redundant with the file's directory location. The runtime derives the source from the load path. The `generated` source layer no longer exists.

**Migration**: Remove `Source` parameter from `RuleSet` record. Callers that need source information track it externally based on which directory the file was loaded from.
