## MODIFIED Requirements

### Requirement: Request routing to dedicated child actors
`SearchActor` SHALL forward each incoming search request to a dedicated
child actor based on request type: `TvSearchRequest` to `TvSearchActor`,
`MovieSearchRequest` to `MovieSearchActor`, `TextSearchRequest` to
`TextSearchActor`.

`MovieSearchActor` SHALL receive `TmdbClient` as a constructor dependency
(in addition to `MediathekClient`, `QualityProbeService`, and `probeLimit`).

#### Scenario: TV search routed to TvSearchActor
- **WHEN** `SearchActor` receives a `TvSearchRequest` and no cached result exists
- **THEN** `SearchActor` SHALL send an internal `ExecuteTvSearch` command to `TvSearchActor`

#### Scenario: Movie search routed to MovieSearchActor
- **WHEN** `SearchActor` receives a `MovieSearchRequest` and no cached result exists
- **THEN** `SearchActor` SHALL send an internal `ExecuteMovieSearch` command to `MovieSearchActor`

#### Scenario: Text search routed to TextSearchActor
- **WHEN** `SearchActor` receives a `TextSearchRequest` and no cached result exists
- **THEN** `SearchActor` SHALL send an internal `ExecuteTextSearch` command to `TextSearchActor`

## ADDED Requirements

### Requirement: MovieSearchActor resolves movie metadata via TMDB before searching
`MovieSearchActor` SHALL attempt to resolve movie metadata from TMDB before querying the Mediathek. When an IMDB ID is provided, it SHALL use `TmdbClient.FindByImdbIdAsync`. When only a text query is provided, it SHALL use `TmdbClient.SearchMovieAsync`. The resolved German title SHALL be used as the Mediathek search term.

#### Scenario: IMDB ID resolved to German title
- **WHEN** `MovieSearchActor` receives an `ExecuteMovieSearch` with `ImdbId = "tt0108052"` and TMDB resolves it to "Schindlers Liste"
- **THEN** `MovieSearchActor` SHALL search the Mediathek with "Schindlers Liste" instead of the raw query

#### Scenario: IMDB ID provided but TMDB unavailable
- **WHEN** `MovieSearchActor` receives an `ExecuteMovieSearch` with `ImdbId = "tt0108052"` but TMDB returns null
- **THEN** `MovieSearchActor` SHALL fall back to searching with the raw `SearchTerm` from the command

#### Scenario: Only text query provided
- **WHEN** `MovieSearchActor` receives an `ExecuteMovieSearch` with no IMDB ID and `SearchTerm = "The Lives of Others"`
- **THEN** `MovieSearchActor` SHALL call `TmdbClient.SearchMovieAsync("The Lives of Others")` and search with the resolved German title if available

#### Scenario: No TMDB key configured (client returns null)
- **WHEN** `TmdbClient` has no API key and always returns null
- **THEN** `MovieSearchActor` SHALL search with the raw `SearchTerm` (graceful degradation to current behavior)

### Requirement: MovieSearchActor populates duration context from TMDB runtime
When TMDB provides a runtime, `MovieSearchActor` SHALL set `MatchContext.ExpectedDurationSeconds` to enable the existing duration filter in `MatchingPipeline`.

#### Scenario: Runtime available from TMDB
- **WHEN** TMDB resolves a movie with `RuntimeMinutes = 195`
- **THEN** `MatchContext.ExpectedDurationSeconds` SHALL be set to `11700` (195 * 60)

#### Scenario: Runtime not available
- **WHEN** TMDB resolves a movie but `RuntimeMinutes` is null
- **THEN** `MatchContext.ExpectedDurationSeconds` SHALL remain null (no duration filtering applied)

### Requirement: MovieSearchActor fallback search with original title
When the German title search yields no Mediathek results and the original title differs from the German title, `MovieSearchActor` SHALL perform a second Mediathek search using the original title and merge the results.

#### Scenario: German title yields no results, original title does
- **WHEN** Mediathek search with "Das Leben der Anderen" returns 0 results and original title is "The Lives of Others"
- **THEN** `MovieSearchActor` SHALL search again with "The Lives of Others" and return those results

#### Scenario: German title already yields results
- **WHEN** Mediathek search with "Schindlers Liste" returns results
- **THEN** `MovieSearchActor` SHALL NOT perform a second search with the original title

#### Scenario: German and original title are identical
- **WHEN** TMDB returns `Title = "Parasite"` and `OriginalTitle = "기생충"` (different) but German title search yields results
- **THEN** no fallback search SHALL be performed
