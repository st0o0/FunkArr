## MODIFIED Requirements

### Requirement: SearchResponse format
The `SearchRequestActor` SHALL respond with `SearchResponse(SearchResult[] Results)` using the `SearchResult` record. `SearchResult` SHALL carry pipeline-resolved metadata: `ImdbId` (from MovieActor/TMDB resolution), `Year` (from TMDB ReleaseYear), and `ResolvedTvdbId` (from ShowActor resolution). These fields SHALL be nullable and populated when the pipeline resolves them.

#### Scenario: Response structure
- **WHEN** a search completes with 5 matched items expanded to 12 quality variants
- **THEN** the response SHALL contain 12 `SearchResult` records sorted by score descending

#### Scenario: Movie search result carries ImdbId and Year
- **WHEN** a movie search for "tt0082096" completes and TMDB resolves title "Das Boot" with ReleaseYear 1981
- **THEN** each `SearchResult` in the response SHALL have `ImdbId = "tt0082096"` and `Year = 1981`

#### Scenario: Query-based movie search resolves ImdbId
- **WHEN** a movie search for query "Das Boot" completes and the MovieActor resolves ImdbId "tt0082096" via TMDB
- **THEN** each `SearchResult` SHALL have `ImdbId = "tt0082096"`

#### Scenario: TV search result carries ResolvedTvdbId
- **WHEN** a TV search for tvdbId 329324 completes
- **THEN** each `SearchResult` in the response SHALL have `ResolvedTvdbId = 329324`

#### Scenario: Query-based TV search resolves TvdbId
- **WHEN** a TV search for query "Tatort" completes and the ShowActor resolves to tvdbId 83214
- **THEN** each `SearchResult` SHALL have `ResolvedTvdbId = 83214`

#### Scenario: Unresolved metadata remains null
- **WHEN** a text search completes (no media actor resolution)
- **THEN** `ImdbId`, `Year`, and `ResolvedTvdbId` SHALL all be null
