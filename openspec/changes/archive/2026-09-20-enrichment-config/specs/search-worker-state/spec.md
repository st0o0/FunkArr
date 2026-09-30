## MODIFIED Requirements

### Requirement: TvSearchWorkerState is a class with Apply methods

TvSearchWorkerState SHALL be a sealed class in FunkArr.Search with private setters. It SHALL expose: SearchId (Guid), ReplyTo (IActorRef), Source (string), Query (string?), Sources (SourceInfo[]), RuleSetId (string?), TvdbId (int?), ImdbId (string?), Season (int?), Limit (int?), Offset (int?), MediaName (string?), Items (EnrichedItem[]), BaseIdentity (MediaIdentity computed from TvdbId/ImdbId), EnrichmentConfig (EnrichmentConfig?).

#### Scenario: Init from SearchSeries command

- **WHEN** Init(SearchSeries, IActorRef) is called
- **THEN** the state SHALL set SearchId, ReplyTo, Source, Query, TvdbId, ImdbId, Season, Limit, Offset from the command, and Sources/Items to empty arrays, and EnrichmentConfig to null
- **AND** the common fields (SearchId, Source, Query, Limit, Offset) SHALL be accessible via the SearchRequest base type

#### Scenario: Apply QueryMediathekCompleted

- **WHEN** Apply(QueryMediathekCompleted) is called
- **THEN** the state SHALL project result.Items to SourceInfo[] via SourceInfo.From and store as Sources

#### Scenario: Apply RuleSetResolved stores EnrichmentConfig

- **WHEN** ApplyRuleSet(ruleSetId, mediaName, enrichmentConfig) is called
- **THEN** the state SHALL set RuleSetId, MediaName, and EnrichmentConfig

#### Scenario: Apply ScoreCompleted preserves ConstructedTitle

- **WHEN** Apply(ScoreCompleted) is called and scored items have MetadataSpec with ConstructedTitle
- **THEN** the state SHALL store the ConstructedTitle per item index for later use in enrichment requests

#### Scenario: Apply EnrichEpisodesCompleted

- **WHEN** Apply(EnrichEpisodesCompleted) is called with resolved episodes
- **THEN** the state SHALL patch Items by updating Identity.Season/Episode and setting Match for each resolved index

### Requirement: TvSearchWorkerState TryGet methods build outgoing messages

The state SHALL provide TryGet methods that decide if a pipeline step is needed AND build the ready-to-send message. Each returns bool and has an out parameter for the message.

#### Scenario: TryGetEnrichmentRequest passes config and ConstructedTitle

- **WHEN** TryGetEnrichmentRequest is called, Items contains entries with Identity.Season=null AND Identity.Episode=null, TvdbId is set, and EnrichmentConfig.Enabled is true
- **THEN** it SHALL return true with an EnrichEpisodes message that includes the EnrichmentConfig and populates each EpisodeCandidate.ConstructedTitle from the stored constructed titles

#### Scenario: TryGetEnrichmentRequest skips when enrichment disabled

- **WHEN** TryGetEnrichmentRequest is called and EnrichmentConfig.Enabled is false
- **THEN** it SHALL return false

#### Scenario: TryGetEnrichmentRequest when all episodes resolved

- **WHEN** TryGetEnrichmentRequest is called and all Items have Season and Episode set
- **THEN** it SHALL return false

#### Scenario: TryGetEnrichmentRequest when no TvdbId

- **WHEN** TryGetEnrichmentRequest is called and TvdbId is null
- **THEN** it SHALL return false

### Requirement: MovieSearchWorkerState stores EnrichmentConfig

MovieSearchWorkerState SHALL store `EnrichmentConfig?` received from `RuleSetResolved`. The `ApplyRuleSet` method SHALL accept and store the enrichment config. `TryGetEnrichmentRequest` SHALL include the `EnrichmentConfig` in the `EnrichMovies` message and SHALL return false when `EnrichmentConfig.Enabled` is false.

#### Scenario: ApplyRuleSet stores EnrichmentConfig

- **WHEN** ApplyRuleSet(ruleSetId, mediaName, enrichmentConfig) is called
- **THEN** the state SHALL store the EnrichmentConfig

#### Scenario: TryGetEnrichmentRequest for movies includes config

- **WHEN** TryGetEnrichmentRequest is called, Items contains matched entries, and ImdbId or TmdbId is set
- **THEN** it SHALL return true with an EnrichMovies message that includes the EnrichmentConfig

#### Scenario: TryGetEnrichmentRequest skips when enrichment disabled

- **WHEN** TryGetEnrichmentRequest is called and EnrichmentConfig.Enabled is false
- **THEN** it SHALL return false
