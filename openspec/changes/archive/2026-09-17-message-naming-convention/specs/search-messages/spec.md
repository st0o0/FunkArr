# search-messages

## MODIFIED Requirements

### Requirement: Search command naming

`TvSearch` SHALL be renamed to `SearchSeries`. `MovieSearch` SHALL be renamed to `SearchMovie`. Each SHALL have its own per-command response type.

#### Scenario: SearchSeries file
- **WHEN** `SearchSeries.cs` is examined
- **THEN** it SHALL contain `SearchSeries`, `SearchSeriesResponse`, `SearchSeriesCompleted`, `SearchSeriesFailed`

#### Scenario: SearchMovie file
- **WHEN** `SearchMovie.cs` is examined
- **THEN** it SHALL contain `SearchMovie`, `SearchMovieResponse`, `SearchMovieCompleted`, `SearchMovieFailed`

## REMOVED Requirements

### Requirement: ISearchResponse domain interface
**Reason**: Replaced by per-command response types (SearchSeriesResponse, SearchMovieResponse).
**Migration**: Replace `Ask<ISearchResponse>` with `Ask<SearchSeriesResponse>` or `Ask<SearchMovieResponse>`.
