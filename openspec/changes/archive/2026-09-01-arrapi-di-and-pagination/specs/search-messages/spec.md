## MODIFIED Requirements

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
