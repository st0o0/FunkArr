## MODIFIED Requirements

### Requirement: Scoring messages use primitive candidates

ScoreItems and ScoreCompleted SHALL use flat primitive records for MatchMagic interaction. ScoreItems SHALL include RequestId and ScoringOrigin for correlation and provenance. ScoreCompleted SHALL include RequestId for response correlation. ScoredItem SHALL carry optional MetadataSpec from identification.

#### Scenario: ScoreItems record

- **WHEN** items are submitted for scoring
- **THEN** ScoreItems SHALL contain: RequestId (Guid), RuleSetId (string), Origin (ScoringOrigin), Candidates (ScoreCandidate[])

#### Scenario: ScoringOrigin record

- **WHEN** a scoring origin is specified
- **THEN** ScoringOrigin SHALL contain: Source (string), Query (string)

#### Scenario: ScoreCandidate record

- **WHEN** a candidate is prepared for scoring
- **THEN** ScoreCandidate SHALL contain: Title (string), Topic (string), Channel (string), Duration (int), Quality (int), Description (string?), Timestamp (long)

#### Scenario: ScoreCompleted record

- **WHEN** scoring completes
- **THEN** ScoreCompleted SHALL contain: RequestId (Guid), Results (ScoredItem[])

#### Scenario: ScoredItem record

- **WHEN** a scored item is returned
- **THEN** ScoredItem SHALL contain: Index (int) referencing the input position, Score (double), Matched (bool), Metadata (MetadataSpec?)

### Requirement: SearchResultItem contains scored media information

SearchResultItem SHALL carry all information needed by the Newznab response formatter, including optional media IDs for *arr result matching. The Title field SHALL contain a scene-style formatted release title built by ReleaseTitleBuilder.

#### Scenario: SearchResultItem fields

- **WHEN** a search result item is created
- **THEN** it SHALL contain: Title (string), Channel (string), Topic (string), Url (string), Duration (int), Size (long), Quality (int), AiredAt (DateTimeOffset?), Score (double), SubtitleUrl (string?), TvdbId (int?), ImdbId (string?), TmdbId (int?)

#### Scenario: Title is scene-formatted

- **WHEN** a search result item is created from a scored Mediathek item with topic "Tatort" and extracted Season "01", Episode "05"
- **THEN** the Title SHALL be a scene-style string like `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Unscored item title

- **WHEN** a search result item is created without scoring (no ruleset loaded)
- **THEN** the Title SHALL still be scene-formatted using available data (topic, title, quality) but without S/E metadata
