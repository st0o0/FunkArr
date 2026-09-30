## MODIFIED Requirements

### Requirement: Unified SearchCommand as public entry point

FunkArr.Messages SHALL define a `SearchCommand` record as the single public command for initiating searches from outside the Search domain. It SHALL define a nested `ISearchParams` marker interface with `TvParams` and `MovieParams` implementations, and a single `Params` property typed as `ISearchParams?`.

#### Scenario: SearchCommand record shape

- **WHEN** a `SearchCommand` is constructed
- **THEN** it SHALL contain: Source (SearchSource), Query (string?), Cat (int?), Limit (int?), Offset (int?), Params (SearchCommand.ISearchParams?)

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

FunkArr.Messages SHALL define an `abstract record SearchRequest` as the base for all search command messages. It SHALL contain the shared fields: SearchId (Guid), Source (SearchSource), Query (string?), Limit (int?), Offset (int?). It SHALL implement IWithSearchId.

#### Scenario: SearchRequest record shape

- **WHEN** a `SearchRequest` subtype is constructed
- **THEN** it SHALL contain: SearchId (Guid), Source (SearchSource), Query (string?), Limit (int?), Offset (int?)
- **AND** it SHALL implement IWithSearchId

#### Scenario: SearchSeries inherits from SearchRequest

- **WHEN** a `SearchSeries` record is constructed
- **THEN** it SHALL inherit from `SearchRequest` and add: Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?)

#### Scenario: SearchMovie inherits from SearchRequest

- **WHEN** a `SearchMovie` record is constructed
- **THEN** it SHALL inherit from `SearchRequest` and add: ImdbId (string?), TmdbId (int?)

### Requirement: Scoring messages use primitive candidates

ScoreItems and ScoreCompleted SHALL use flat primitive records for Scoring interaction. ScoreItems SHALL include RequestId and ScoringOrigin for correlation and provenance. ScoreCompleted SHALL include RequestId for response correlation.

#### Scenario: ScoringOrigin record

- **WHEN** a scoring origin is specified
- **THEN** ScoringOrigin SHALL contain: Source (SearchSource), Query (string)
