## ADDED Requirements

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

## MODIFIED Requirements

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
