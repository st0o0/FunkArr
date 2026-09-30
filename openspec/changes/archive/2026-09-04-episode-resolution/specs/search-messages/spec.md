## MODIFIED Requirements

### Requirement: SearchResultItem contains scored media information
SearchResultItem SHALL carry all information needed by the Newznab response formatter, including optional media IDs for *arr result matching, and optional episode resolution metadata. The Title field SHALL contain a scene-style formatted release title built by ReleaseTitleBuilder.

#### Scenario: SearchResultItem fields
- **WHEN** a search result item is created
- **THEN** it SHALL contain: Title (string), Channel (string), Topic (string), Url (string), Duration (int), Size (long), Quality (int), AiredAt (DateTimeOffset?), Score (double), SubtitleUrl (string?), TvdbId (int?), ImdbId (string?), TmdbId (int?), Season (string?), Episode (string?), ResolutionConfidence (float?), ResolutionStrategy (string?)

#### Scenario: Title is scene-formatted
- **WHEN** a search result item is created from a scored Mediathek item with topic "Tatort" and extracted Season "01", Episode "05"
- **THEN** the Title SHALL be a scene-style string like `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Unscored item title
- **WHEN** a search result item is created without scoring (no ruleset loaded)
- **THEN** the Title SHALL still be scene-formatted using available data (topic, title, quality) but without S/E metadata

#### Scenario: Item with resolution metadata
- **WHEN** a search result item was resolved via FuzzyTitleMatch with confidence 0.85
- **THEN** ResolutionConfidence SHALL be 0.85 and ResolutionStrategy SHALL be "FuzzyTitleMatch"

#### Scenario: Item without resolution
- **WHEN** a search result item has season/episode from regex extraction
- **THEN** ResolutionConfidence SHALL be null and ResolutionStrategy SHALL be null (regex extraction is not resolution)
