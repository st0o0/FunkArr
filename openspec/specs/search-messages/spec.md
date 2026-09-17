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

### Requirement: SearchRequest abstract base record

FunkArr.Messages SHALL define an `abstract record SearchRequest` as the base for all search command messages. It SHALL contain the shared fields: SearchId (Guid), Source (string), Query (string?), Limit (int?), Offset (int?). It SHALL implement IWithSearchId.

#### Scenario: SearchRequest record shape

- **WHEN** a `SearchRequest` subtype is constructed
- **THEN** it SHALL contain: SearchId (Guid), Source (string), Query (string?), Limit (int?), Offset (int?)
- **AND** it SHALL implement IWithSearchId

#### Scenario: SearchSeries inherits from SearchRequest

- **WHEN** a `SearchSeries` record is constructed
- **THEN** it SHALL inherit from `SearchRequest` and add: Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?)

#### Scenario: SearchMovie inherits from SearchRequest

- **WHEN** a `SearchMovie` record is constructed
- **THEN** it SHALL inherit from `SearchRequest` and add: ImdbId (string?), TmdbId (int?)

### Requirement: Search command messages use primitive types only

Search command and response messages SHALL use only primitive types and simple records. SearchSeries SHALL inherit from SearchRequest and contain: Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?). SearchMovie SHALL inherit from SearchRequest and contain: ImdbId (string?), TmdbId (int?). SearchSeriesCompleted SHALL contain: SearchId (Guid), Items (SearchResultItem[]), Total (int). SearchSeriesFailed SHALL contain: SearchId (Guid), Cause (Exception). SearchMovieCompleted SHALL contain: SearchId (Guid), Items (SearchResultItem[]), Total (int). SearchMovieFailed SHALL contain: SearchId (Guid), Cause (Exception).

#### Scenario: SearchSeries message

- **WHEN** Sonarr searches for a show
- **THEN** a SearchSeries record SHALL be created inheriting shared fields from SearchRequest and adding TV-specific fields

#### Scenario: SearchMovie message

- **WHEN** Radarr searches for a movie
- **THEN** a SearchMovie record SHALL be created inheriting shared fields from SearchRequest and adding movie-specific fields

#### Scenario: SearchSeriesFailed carries Exception

- **WHEN** a TV search fails for any reason (timeout, query failure, scoring failure)
- **THEN** SearchSeriesFailed SHALL contain the Exception as Cause, not a string Reason

#### Scenario: SearchMovieFailed carries Exception

- **WHEN** a movie search fails for any reason (timeout, query failure, scoring failure)
- **THEN** SearchMovieFailed SHALL contain the Exception as Cause, not a string Reason

### Requirement: SearchResultItem contains scored media information

SearchResultItem SHALL carry all information needed by the Newznab response formatter, including optional media IDs for *arr result matching, and optional enrichment metadata. The SearchResultItem record SHALL include optional MatchConfidence (float?) and MatchMethod (MatchMethod?) fields. These fields SHALL be populated for both TV show and movie search results when metadata enrichment is performed. The Title field SHALL contain a scene-style formatted release title built by ReleaseTitleBuilder.

#### Scenario: SearchResultItem fields

- **WHEN** a search result item is created
- **THEN** it SHALL contain: Title (string), Channel (string), Topic (string), Url (string), Duration (int), Size (long), Quality (int), AiredAt (DateTimeOffset?), Score (double), SubtitleUrl (string?), TvdbId (int?), ImdbId (string?), TmdbId (int?), Season (string?), Episode (string?), MatchConfidence (float?), MatchMethod (MatchMethod?)

#### Scenario: Title is scene-formatted

- **WHEN** a search result item is created from a scored Mediathek item with topic "Tatort" and extracted Season "01", Episode "05"
- **THEN** the Title SHALL be a scene-style string like `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Unscored item title

- **WHEN** a search result item is created without scoring (no ruleset loaded)
- **THEN** the Title SHALL still be scene-formatted using available data (topic, title, quality) but without S/E metadata

#### Scenario: TV result with enrichment metadata

- **WHEN** a TV search result is enriched via TitleMatch with confidence 0.85
- **THEN** the SearchResultItem SHALL have MatchConfidence=0.85 and MatchMethod=MatchMethod.TitleMatch

#### Scenario: Movie result with enrichment metadata

- **WHEN** a movie search result is enriched via TitleMatch with confidence 1.0
- **THEN** the SearchResultItem SHALL have MatchConfidence=1.0 and MatchMethod=MatchMethod.TitleMatch

#### Scenario: Unenriched result

- **WHEN** a search result was not enriched (no TVDB/TMDB key or enrichment failed)
- **THEN** the SearchResultItem SHALL have MatchConfidence=null and MatchMethod=null

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

ScoreItems and ScoreCompleted SHALL use flat primitive records for Scoring interaction. ScoreItems SHALL include RequestId and ScoringOrigin for correlation and provenance. ScoreCompleted SHALL include RequestId for response correlation.

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

### Requirement: RuleSet response types use abstract record base

FunkArr.Messages.RuleSet SHALL define an `abstract record RuleSetResponse` as the base for all RuleSet resolution responses. `RuleSetResolved` and `RuleSetFailed` SHALL extend `RuleSetResponse`. The `IRuleSetResponse` marker interface SHALL be removed.

#### Scenario: RuleSetResponse base type

- **WHEN** a caller uses `Ask<RuleSetResponse>` to resolve a RuleSet
- **THEN** the result SHALL be pattern-matched on `RuleSetResolved` or `RuleSetFailed`

### Requirement: RuleSetFailed replaces RuleSetNotFound

FunkArr.Messages.RuleSet SHALL define a `RuleSetFailed` sealed record containing `Cause (Exception)`, replacing `RuleSetNotFound(string TopicOrAlias)`. A custom `RuleSetNotFoundException` SHALL carry the `TopicOrAlias` information. `RuleSetFailed` SHALL extend `RuleSetResponse`.

#### Scenario: RuleSet not found

- **WHEN** the RuleSetResolver cannot find a matching ruleset for topic "Unknown Show"
- **THEN** it SHALL return `RuleSetFailed(new RuleSetNotFoundException("Unknown Show"))`

#### Scenario: RuleSet resolution error

- **WHEN** the RuleSet resolution fails due to an unexpected exception
- **THEN** the PipeTo failure handler SHALL return `RuleSetFailed(ex)` with the original exception

#### Scenario: RuleSetNotFoundException carries topic

- **WHEN** a `RuleSetFailed` is received with `Cause` of type `RuleSetNotFoundException`
- **THEN** the exception SHALL expose `TopicOrAlias` property with the original search term

### Requirement: RuleSetResolverState.Resolve returns typed response

The `RuleSetResolverState.Resolve()` method SHALL return `RuleSetResponse` instead of `object`, enabling compile-time type safety.

#### Scenario: Typed resolve return

- **WHEN** `RuleSetResolverState.Resolve()` is called
- **THEN** it SHALL return either `RuleSetResolved` or `RuleSetFailed`, both subtypes of `RuleSetResponse`
