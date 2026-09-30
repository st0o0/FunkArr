## MODIFIED Requirements

### Requirement: Search command messages use primitive types only

All search command and response messages SHALL be defined as sealed records in FunkArr.Messages with primitive parameter types only (string, int, long, double, bool, Guid, DateTimeOffset, arrays of records). No IActorRef, no external domain types.

Internal shard-routed messages SHALL drop the `Command` suffix per the naming convention.

#### Scenario: SearchCommand record

- **WHEN** a search is initiated from outside the Search domain
- **THEN** SearchCommand SHALL contain: Query (string?), Cat (int?), Limit (int?), Offset (int?), Params (SearchCommand.ISearchParams?)
- **AND** SearchCommand SHALL keep its name (public entry point exception)

#### Scenario: TvSearch record (renamed from TvSearchCommand)

- **WHEN** the SearchManager routes to a TV search shard
- **THEN** `TvSearch` SHALL contain: SearchId (Guid), Query (string?), Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?), Limit (int?), Offset (int?)
- **AND** `TvSearch` SHALL implement only IWithSearchId

#### Scenario: MovieSearch record (renamed from MovieSearchCommand)

- **WHEN** the SearchManager routes to a movie search shard
- **THEN** `MovieSearch` SHALL contain: SearchId (Guid), Query (string?), ImdbId (string?), TmdbId (int?), Limit (int?), Offset (int?)
- **AND** `MovieSearch` SHALL implement only IWithSearchId
