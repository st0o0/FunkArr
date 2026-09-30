## MODIFIED Requirements

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
- **THEN** SearchFailed SHALL contain: SearchId (Guid), Reason (string), Cause (Exception?)
- **AND** SearchFailed SHALL implement ISearchResponse
- **AND** Cause SHALL default to null for backward compatibility with string-only failure paths
