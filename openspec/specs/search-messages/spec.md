## ADDED Requirements

### Requirement: Unified SearchCommand as public entry point

FunkArr.Messages SHALL define a `SearchCommand` record as the single public command for initiating searches from outside the Search domain. It SHALL define a nested `ISearchParams` marker interface with `TvParams` and `MovieParams` implementations, and a single `Params` property typed as `ISearchParams?`.

#### Scenario: SearchCommand record shape

- **WHEN** a `SearchCommand` is constructed
- **THEN** it SHALL contain: Query (string?), Cat (int?), Limit (int?), Offset (int?), Params (SearchCommand.ISearchParams?)

#### Scenario: ISearchParams marker interface

- **WHEN** `SearchCommand.ISearchParams` is defined
- **THEN** it SHALL be an empty interface nested inside `SearchCommand`
- **AND** `SearchCommand.TvParams` and `SearchCommand.MovieParams` SHALL implement it

#### Scenario: TvParams nested record

- **WHEN** a TV search is requested
- **THEN** `SearchCommand.TvParams` SHALL contain: Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?)
- **AND** it SHALL implement `SearchCommand.ISearchParams`

#### Scenario: MovieParams nested record

- **WHEN** a movie search is requested
- **THEN** `SearchCommand.MovieParams` SHALL contain: ImdbId (string?), TmdbId (int?)
- **AND** it SHALL implement `SearchCommand.ISearchParams`

#### Scenario: TV search via SearchCommand

- **WHEN** a TV search is initiated
- **THEN** `SearchCommand` SHALL have `Params` set to a `TvParams` instance

#### Scenario: Movie search via SearchCommand

- **WHEN** a movie search is initiated
- **THEN** `SearchCommand` SHALL have `Params` set to a `MovieParams` instance

#### Scenario: General search via SearchCommand

- **WHEN** a general search is initiated
- **THEN** `SearchCommand` SHALL have `Params` set to null

### Requirement: Search command messages use primitive types only

All search command and response messages SHALL be defined as sealed records in FunkArr.Messages with primitive parameter types only (string, int, long, double, bool, Guid, DateTimeOffset, arrays of records). No IActorRef, no external domain types.

#### Scenario: SearchCommand record

- **WHEN** a search is initiated from outside the Search domain
- **THEN** SearchCommand SHALL contain: Query (string?), Cat (int?), Limit (int?), Offset (int?), Params (SearchCommand.ISearchParams?)

#### Scenario: TvSearchCommand record

- **WHEN** the SearchManager routes to a TV search shard
- **THEN** TvSearchCommand SHALL contain: SearchId (Guid), Query (string?), Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?), Limit (int?), Offset (int?)
- **AND** TvSearchCommand SHALL implement only IWithSearchId (not ISearchCommand)

#### Scenario: MovieSearchCommand record

- **WHEN** the SearchManager routes to a movie search shard
- **THEN** MovieSearchCommand SHALL contain: SearchId (Guid), Query (string?), ImdbId (string?), TmdbId (int?), Limit (int?), Offset (int?)
- **AND** MovieSearchCommand SHALL implement only IWithSearchId (not ISearchCommand)

#### Scenario: SearchCompleted record

- **WHEN** a search succeeds
- **THEN** SearchCompleted SHALL contain: SearchId (Guid), Items (SearchResultItem[]), Total (int)
- **AND** SearchCompleted SHALL implement ISearchResponse

#### Scenario: SearchFailed record

- **WHEN** a search fails
- **THEN** SearchFailed SHALL contain: SearchId (Guid), Reason (string)
- **AND** SearchFailed SHALL implement ISearchResponse

### Requirement: SearchResultItem contains scored media information

SearchResultItem SHALL carry all information needed by the Newznab response formatter, including optional media IDs for *arr result matching, and optional resolution metadata. The SearchResultItem record SHALL include optional ResolutionConfidence (float?) and ResolutionStrategy (string?) fields. These fields SHALL be populated for both TV show and movie search results when metadata resolution is performed. The Title field SHALL contain a scene-style formatted release title built by ReleaseTitleBuilder.

#### Scenario: SearchResultItem fields

- **WHEN** a search result item is created
- **THEN** it SHALL contain: Title (string), Channel (string), Topic (string), Url (string), Duration (int), Size (long), Quality (int), AiredAt (DateTimeOffset?), Score (double), SubtitleUrl (string?), TvdbId (int?), ImdbId (string?), TmdbId (int?), Season (string?), Episode (string?), ResolutionConfidence (float?), ResolutionStrategy (string?)

#### Scenario: Title is scene-formatted

- **WHEN** a search result item is created from a scored Mediathek item with topic "Tatort" and extracted Season "01", Episode "05"
- **THEN** the Title SHALL be a scene-style string like `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Unscored item title

- **WHEN** a search result item is created without scoring (no ruleset loaded)
- **THEN** the Title SHALL still be scene-formatted using available data (topic, title, quality) but without S/E metadata

#### Scenario: TV result with resolution metadata

- **WHEN** a TV search result is resolved via FuzzyTitleMatch with confidence 0.85
- **THEN** the SearchResultItem SHALL have ResolutionConfidence=0.85 and ResolutionStrategy="FuzzyTitleMatch"

#### Scenario: Movie result with resolution metadata

- **WHEN** a movie search result is resolved via TmdbIdLookup with confidence 1.0
- **THEN** the SearchResultItem SHALL have ResolutionConfidence=1.0 and ResolutionStrategy="TmdbIdLookup"

#### Scenario: Unresolved result

- **WHEN** a search result was not resolved (no TVDB/TMDB key or resolution failed)
- **THEN** the SearchResultItem SHALL have ResolutionConfidence=null and ResolutionStrategy=null

#### Scenario: Movie result with TMDB-validated year

- **WHEN** a movie result is enriched via TMDB resolution
- **THEN** the release title SHALL include the validated year from TMDB

### Requirement: Mediathek messages model the external API contract

MediathekQuery and MediathekQueryCompleted SHALL model the MediathekViewWeb API request and response as primitive records.

#### Scenario: MediathekQuery record

- **WHEN** a query to MediathekViewWeb is constructed
- **THEN** MediathekQuery SHALL contain: Fields (MediathekQueryField[]), SortBy (string?), SortOrder (string?), Future (bool), Offset (int), Size (int), DurationMin (int?), DurationMax (int?)

#### Scenario: MediathekQueryField record

- **WHEN** a query field is specified
- **THEN** MediathekQueryField SHALL contain: Fields (string[]) for searchable field names and Query (string) for the search term

#### Scenario: MediathekQueryCompleted record

- **WHEN** a query succeeds
- **THEN** MediathekQueryCompleted SHALL contain: Items (MediathekItem[]), Total (int)

#### Scenario: MediathekItem record with all quality variants

- **WHEN** a Mediathek result item is mapped
- **THEN** MediathekItem SHALL contain: Channel (string), Topic (string), Title (string), Description (string?), Timestamp (long), Duration (int), Size (long), UrlVideoLow (string?), UrlVideo (string?), UrlVideoHd (string?), UrlSubtitle (string?), UrlWebsite (string?)

### Requirement: Scoring messages use primitive candidates

ScoreItems and ScoreCompleted SHALL use flat primitive records for MatchMagic interaction. ScoreItems SHALL include RequestId and ScoringOrigin for correlation and provenance. ScoreCompleted SHALL include RequestId for response correlation.

#### Scenario: ScoreItems record

- **WHEN** items are submitted for scoring
- **THEN** ScoreItems SHALL contain: RequestId (Guid), RuleSetId (string), Origin (ScoringOrigin), Candidates (ScoreCandidate[])

#### Scenario: ScoringOrigin record

- **WHEN** a scoring origin is specified
- **THEN** ScoringOrigin SHALL contain: Source (string), Query (string)

#### Scenario: ScoreCandidate record

- **WHEN** a candidate is prepared for scoring
- **THEN** ScoreCandidate SHALL contain: Title (string), Topic (string), Channel (string), Duration (int), Quality (int), Description (string?), Timestamp (long)

#### Scenario: ScoreCompleted record

- **WHEN** scoring completes
- **THEN** ScoreCompleted SHALL contain: RequestId (Guid), Results (ScoredItem[])

#### Scenario: ScoredItem record

- **WHEN** a scored item is returned
- **THEN** ScoredItem SHALL contain: Index (int) referencing the input position, Score (double), Matched (bool), Metadata (MetadataSpec?)

### Requirement: IWithSearchId interface for shard routing

FunkArr.Messages SHALL define an IWithSearchId interface with a Guid SearchId property, analogous to the existing IWithDownloadId. TvSearchCommand and MovieSearchCommand SHALL implement this interface.

#### Scenario: Shard key extraction

- **WHEN** the ShardMessageExtractor receives a message implementing IWithSearchId
- **THEN** it SHALL extract the SearchId as the entity id for shard routing

### Requirement: ResolveRuleSet supports ID-based lookup

The `ResolveRuleSet` message SHALL accept optional media ID fields alongside the existing topic/alias string for ID-based resolution.

#### Scenario: ResolveRuleSet with IDs

- **WHEN** a resolve request is constructed
- **THEN** `ResolveRuleSet` SHALL contain: TopicOrAlias (string?), TvdbId (int?), ImdbId (string?), TmdbId (int?)

### Requirement: RuleSetResolved includes topic

The `RuleSetResolved` response SHALL include the topic string so callers can use it for subsequent queries without a second resolver round-trip.

#### Scenario: RuleSetResolved fields

- **WHEN** a ruleset is resolved
- **THEN** `RuleSetResolved` SHALL contain: RuleSetId (string), Topic (string)

### Requirement: RegisterRuleSet includes media IDs

The `RegisterRuleSet` message SHALL include optional media ID fields for ID-based resolver indexing.

#### Scenario: RegisterRuleSet fields

- **WHEN** a ruleset is registered
- **THEN** `RegisterRuleSet` SHALL contain: RuleSetId (string), Topic (string), Aliases (string[]), TvdbId (int?), ImdbId (string?), TmdbId (int?)
