## ADDED Requirements

### Requirement: Search command messages use primitive types only

All search command and response messages SHALL be defined as sealed records in FunkArr.Messages with primitive parameter types only (string, int, long, double, bool, Guid, DateTimeOffset, arrays of records). No IActorRef, no external domain types.

#### Scenario: TvSearchCommand record

- **WHEN** a TV search is initiated
- **THEN** TvSearchCommand SHALL contain: SearchId (Guid), Query (string?), Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?), Limit (int?), Offset (int?)

#### Scenario: MovieSearchCommand record

- **WHEN** a movie search is initiated
- **THEN** MovieSearchCommand SHALL contain: SearchId (Guid), Query (string?), ImdbId (string?), TmdbId (int?), Limit (int?), Offset (int?)

#### Scenario: GeneralSearchCommand record

- **WHEN** a general search is initiated
- **THEN** GeneralSearchCommand SHALL contain: Query (string?), Cat (int?), Limit (int?), Offset (int?)

#### Scenario: SearchCompleted record

- **WHEN** a search succeeds
- **THEN** SearchCompleted SHALL contain: SearchId (Guid), Items (SearchResultItem[]), Total (int)

#### Scenario: SearchFailed record

- **WHEN** a search fails
- **THEN** SearchFailed SHALL contain: SearchId (Guid), Reason (string)

### Requirement: SearchResultItem contains scored media information

SearchResultItem SHALL carry all information needed by the Newznab response formatter, including optional media IDs for *arr result matching.

#### Scenario: SearchResultItem fields

- **WHEN** a search result item is created
- **THEN** it SHALL contain: Title (string), Channel (string), Topic (string), Url (string), Duration (int), Size (long), Quality (int), AiredAt (DateTimeOffset?), Score (double), TvdbId (int?), ImdbId (string?), TmdbId (int?)

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
- **THEN** ScoredItem SHALL contain: Index (int) referencing the input position, Score (double), Matched (bool)

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
