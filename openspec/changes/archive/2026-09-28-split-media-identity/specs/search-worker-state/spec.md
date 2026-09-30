## MODIFIED Requirements

### Requirement: TvSearchWorkerState is a class with Apply methods

TvSearchWorkerState SHALL be a sealed class in FunkArr.Search with private setters. BaseIdentity SHALL return a ShowIdentity with TvdbId, ImdbId, Season=null, Episode=null. Apply(ScoreCompleted) SHALL build EnrichedItems with ShowIdentity, patching Season and Episode from MetadataSpec. Apply(EnrichEpisodesCompleted) SHALL override ShowIdentity.Season and ShowIdentity.Episode with enrichment results even when they were previously set by regex extraction, since enrichment results are authoritative TVDB numbers.

#### Scenario: BaseIdentity returns ShowIdentity

- **WHEN** TvSearchWorkerState has TvdbId=83214 and ImdbId="tt0806910"
- **THEN** BaseIdentity SHALL return ShowIdentity("tt0806910", 83214, null, null)

#### Scenario: Apply ScoreCompleted builds ShowIdentity with Season/Episode

- **WHEN** Apply(ScoreCompleted) is called and scored items have MetadataSpec with Season="2", Episode="5"
- **THEN** the EnrichedItem SHALL have Identity as ShowIdentity with Season="2", Episode="5"

#### Scenario: Apply ScoreCompleted preserves ConstructedTitle

- **WHEN** Apply(ScoreCompleted) is called and scored items have MetadataSpec with ConstructedTitle
- **THEN** the state SHALL store the ConstructedTitle per item index for later use in enrichment requests

#### Scenario: Apply EnrichEpisodesCompleted overrides regex S/E

- **WHEN** Apply(EnrichEpisodesCompleted) is called with resolved episodes
- **AND** an item already had Season/Episode from regex extraction
- **THEN** the state SHALL override ShowIdentity.Season and ShowIdentity.Episode with the enrichment result
- **AND** set Match with the enrichment confidence and method

### Requirement: MovieSearchWorkerState is a class with Apply methods

MovieSearchWorkerState SHALL be a sealed class in FunkArr.Search with private setters. BaseIdentity SHALL return a MovieIdentity with ImdbId, TmdbId, Year=null. Apply(ScoreCompleted) SHALL build EnrichedItems with MovieIdentity. Season and Episode from MetadataSpec SHALL NOT be transferred to MovieIdentity. Year SHALL be derived from the source's AiredAt or enrichment data.

#### Scenario: BaseIdentity returns MovieIdentity

- **WHEN** MovieSearchWorkerState has ImdbId="tt0137523" and TmdbId=550
- **THEN** BaseIdentity SHALL return MovieIdentity("tt0137523", 550, null)

#### Scenario: Apply ScoreCompleted builds MovieIdentity without Season/Episode

- **WHEN** Apply(ScoreCompleted) is called and scored items have MetadataSpec with Season="1", Episode="2"
- **THEN** the EnrichedItem SHALL have Identity as MovieIdentity without Season or Episode fields

#### Scenario: Apply MoviesEnriched patches MovieIdentity

- **WHEN** Apply(MoviesEnriched) is called with ImdbId="tt0137523" and TmdbId=550
- **THEN** the EnrichedItem SHALL have MovieIdentity updated with the enriched ImdbId and TmdbId
