## MODIFIED Requirements

### Requirement: SearchResultItem contains scored media information

SearchResultItem SHALL carry all information needed by the Newznab response formatter, including optional media IDs for *arr result matching, and optional enrichment metadata. The SearchResultItem record SHALL include optional MatchConfidence (float?) and MatchMethod (MatchMethod?) fields. These fields SHALL be populated for both TV show and movie search results when metadata enrichment is performed. The Title field SHALL contain a scene-style formatted release title built by ReleaseTitleBuilder.

#### Scenario: SearchResultItem fields

- **WHEN** a search result item is created
- **THEN** it SHALL contain: Title (string), Channel (string), Topic (string), Url (string), Duration (int), Size (long), Quality (int), AiredAt (DateTimeOffset?), Score (double), SubtitleUrl (string?), TvdbId (int?), ImdbId (string?), TmdbId (int?), Season (string?), Episode (string?), MatchConfidence (float?), MatchMethod (MatchMethod?)

#### Scenario: Title is scene-formatted

- **WHEN** a search result item is created from a scored Mediathek item with topic "Tatort" and extracted Season "01", Episode "05"
- **THEN** the Title SHALL be a scene-style string like `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Unscored item title

- **WHEN** a search result item is created without scoring (no ruleset loaded)
- **THEN** the Title SHALL still be scene-formatted using available data (topic, title, quality) but without S/E metadata

#### Scenario: TV result with enrichment metadata

- **WHEN** a TV search result is enriched via TitleMatch with confidence 0.85
- **THEN** the SearchResultItem SHALL have MatchConfidence=0.85 and MatchMethod=MatchMethod.TitleMatch

#### Scenario: Movie result with enrichment metadata

- **WHEN** a movie search result is enriched via TitleMatch with confidence 1.0
- **THEN** the SearchResultItem SHALL have MatchConfidence=1.0 and MatchMethod=MatchMethod.TitleMatch

#### Scenario: Unenriched result

- **WHEN** a search result was not enriched (no TVDB/TMDB key or enrichment failed)
- **THEN** the SearchResultItem SHALL have MatchConfidence=null and MatchMethod=null
