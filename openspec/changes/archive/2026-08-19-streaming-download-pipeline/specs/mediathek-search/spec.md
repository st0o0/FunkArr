## MODIFIED Requirements

### Requirement: Title matching
The system SHALL match search results against expected titles using a composed matching pipeline. Title normalization MUST handle German special characters (umlauts, eszett) and common variations. The pipeline SHALL be a pure function chain operating on a `MatchContext` that carries the search parameters.

#### Scenario: Title with umlauts matches
- **WHEN** the expected title is "Uber den Dachern" and a result has topic "Über den Dächern"
- **THEN** the normalized comparison considers them a match

#### Scenario: Skip keywords
- **WHEN** a search result title contains "Audiodeskription", "Trailer", "Gebardensprache", or other configured skip keywords
- **THEN** the result is excluded from the output

#### Scenario: Show name matched against Topic field
- **WHEN** the MatchContext contains ShowName "Tatort" and a MediathekViewWeb result has Topic "Tatort"
- **THEN** the result passes the show matching filter

#### Scenario: Partial title match with prefix
- **WHEN** the expected title is at least 13 characters and the first 13 normalized characters match a result's topic
- **THEN** the result passes the title matching filter

### Requirement: Episode pattern matching
The system SHALL identify episodes using multiple strategies beyond S##E## patterns: air date comparison, "Folge" numbering extraction from titles, and title-embedded episode identifiers.

#### Scenario: S##E## pattern in title
- **WHEN** a result title contains "S01E03" and the MatchContext specifies season 1 episode 3
- **THEN** the pattern match confirms the result

#### Scenario: Air date match
- **WHEN** the MatchContext contains an AirDate of 2025-03-15 and a result's description contains "15.03.2025"
- **THEN** the date match confirms the result

#### Scenario: No episode filter when no season/episode/date provided
- **WHEN** the MatchContext has no Season, Episode, or AirDate set
- **THEN** the episode matching filter is skipped and all show-matched results pass through

### Requirement: Composed matching pipeline
The system SHALL compose matching logic as a `MatchingPipeline.Execute()` LINQ chain that accepts `IEnumerable<MediathekResultItem>` and a `MatchContext`, returning scored and ranked `SearchResult` items.

#### Scenario: Full pipeline execution
- **WHEN** `MatchingPipeline.Execute()` is called with 100 MediathekViewWeb results and a TV search MatchContext
- **THEN** results are filtered (junk removed, show matched, episode matched, duration checked), expanded into quality variants, scored, and returned ordered by score descending

#### Scenario: MatchContext carries all search parameters
- **WHEN** a TvSearchRequest is received with TvdbId, ShowName, Season, Episode
- **THEN** the SearchActor constructs a `MatchContext` with all available fields before calling the pipeline

#### Scenario: Empty results pass through cleanly
- **WHEN** no MediathekViewWeb results match the filters
- **THEN** `MatchingPipeline.Execute()` returns an empty list

### Requirement: TVDB metadata lookup
The system SHALL look up show metadata (name, episode titles, air dates) from TheTVDB using the TVDB ID provided by Sonarr/Radarr. The lookup MUST happen once per search request in the SearchActor, before the matching pipeline runs.

#### Scenario: TVDB lookup for show name
- **WHEN** a tvsearch request includes `tvdbid=12345` and no `q` parameter
- **THEN** the SearchActor resolves the TVDB ID to the show's German title and uses it as the MediathekViewWeb search term and MatchContext.ShowName

#### Scenario: TVDB lookup failure
- **WHEN** the TVDB API is unavailable or the ID is unknown
- **THEN** the system falls back to the `q` parameter text if provided, or returns empty results

#### Scenario: TVDB episode air date enrichment
- **WHEN** a tvsearch request specifies season and episode and TVDB data is available
- **THEN** the SearchActor retrieves the episode's air date from TVDB and includes it in the MatchContext for date-based matching

### Requirement: Result scoring
The system SHALL assign a relevance score to each matched result based on weighted criteria: title closeness, date proximity, and quality tier.

#### Scenario: Higher quality scores higher
- **WHEN** two results match the same episode but one is 1080p and the other 720p
- **THEN** the 1080p result has a higher score

#### Scenario: Closer air date scores higher
- **WHEN** two results match the same show and the MatchContext includes an AirDate
- **THEN** the result whose timestamp is closer to the expected air date scores higher
