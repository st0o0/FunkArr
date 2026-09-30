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

## REMOVED Requirements

### Requirement: GeneralSearchCommand record

**Reason**: Replaced by SearchCommand with Tv=null and Movie=null
**Migration**: Use `new SearchCommand(query, cat, limit, offset, null)` instead

### Requirement: IWithSearchId interface for shard routing

**Reason**: The requirement text is unchanged — IWithSearchId stays. Only the constraint that TvSearchCommand and MovieSearchCommand implement ISearchCommand is removed. They SHALL implement only IWithSearchId.
**Migration**: Remove ISearchCommand from TvSearchCommand and MovieSearchCommand declarations

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
- **THEN** SearchFailed SHALL contain: SearchId (Guid), Reason (string)
- **AND** SearchFailed SHALL implement ISearchResponse
