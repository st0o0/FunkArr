## MODIFIED Requirements

### Requirement: Newznab XML contract tests
The system SHALL have Verify-based snapshot tests that validate Newznab XML output stability for Prowlarr compatibility.

#### Scenario: Caps response snapshot
- **WHEN** `NewznabSerializer` generates a caps response
- **THEN** the XML SHALL match the approved `.verified.txt` snapshot and contain `<limits max="100" default="100" />`, `<categories>` with TV and Movie categories, and `<searching>` with search, tv-search, and movie-search capabilities

#### Scenario: TV search response snapshot
- **WHEN** `NewznabResultMapper` generates a TV search response with representative `SearchResult` data including `ResolvedTvdbId`
- **THEN** the XML SHALL match the approved snapshot and contain `<newznab:attr>` elements for category, size, resolution, video, tvdbid, season, episode

#### Scenario: Movie search response snapshot
- **WHEN** `NewznabResultMapper` generates a movie search response with representative data including `ImdbId` and `Year`
- **THEN** the XML SHALL match the approved snapshot and contain `<newznab:attr>` elements for category, size, resolution, video, imdbid, year

#### Scenario: Empty search results snapshot
- **WHEN** `NewznabSerializer` generates a search response with no results
- **THEN** the XML SHALL match the approved snapshot with an empty `<channel>` element
